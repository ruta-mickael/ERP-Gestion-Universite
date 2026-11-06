namespace erp_univ_back.Features.Inscription.Register;

using erp_univ_back.Features.Etudiant;
using erp_univ_back.Features.Inscription;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using erp_univ_back.Shared.Utils;
using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;

public interface IRegisterInscriptionService
{
    Task<RegisterInscriptionResponse> ExecuteAsync(RegisterInscriptionRequest request, CancellationToken ct = default);
}

public class RegisterInscriptionService(
    ErpUnivDbContext db,
    IValidator<RegisterInscriptionRequest> validator,
    IMatriculeGenerator matriculeGenerator) : IRegisterInscriptionService
{
    public async Task<RegisterInscriptionResponse> ExecuteAsync(RegisterInscriptionRequest request, CancellationToken ct = default)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First().ErrorMessage;
            throw new BadRequestException(firstError);
        }

        var annee = await db.AnneeAcademiques.FirstOrDefaultAsync(a => a.Id == request.IdAnneeAcademique, ct)
            ?? throw new NotFoundException($"L'année académique avec l'identifiant '{request.IdAnneeAcademique}' n'existe pas.");

        if (!annee.EstActive)
        {
            throw new BadRequestException($"L'année académique '{annee.Libelle}' n'est pas active. Les inscriptions ne sont possibles que sur une année académique active.");
        }

        var classe = await db.FiliereNiveaux
            .Include(fn => fn.Niveau)
                .ThenInclude(n => n.Cycle)
            .Include(fn => fn.Filiere)
            .FirstOrDefaultAsync(fn => fn.Id == request.IdFiliereNiveau, ct);

        if (classe is null)
        {
            throw new NotFoundException($"La classe (filière-niveau) avec l'identifiant '{request.IdFiliereNiveau}' n'existe pas.");
        }

        if (await db.Etudiants.AnyAsync(e => e.NumeroTelephone == request.EtudiantInfos.NumeroTelephone, ct))
        {
            throw new ConflictException($"Le numéro de téléphone '{request.EtudiantInfos.NumeroTelephone}' est déjà enregistré.");
        }

        if (await db.Etudiants.AnyAsync(e => e.Email == request.EtudiantInfos.Email, ct))
        {
            throw new ConflictException($"L'adresse email '{request.EtudiantInfos.Email}' est déjà utilisée.");
        }

        if (!string.IsNullOrWhiteSpace(request.EtudiantInfos.NumeroCin) &&
            await db.Etudiants.AnyAsync(e => e.NumeroCin == request.EtudiantInfos.NumeroCin, ct))
        {
            throw new ConflictException($"Le numéro CIN '{request.EtudiantInfos.NumeroCin}' est déjà utilisé.");
        }

        var matricule = await matriculeGenerator.GenererAsync(true, ct);

        var etudiant = request.EtudiantInfos.Adapt<EtudiantEntity>();
        etudiant.Matricule = matricule;
        etudiant.EtudiantParents = [];

        foreach (var parentDto in request.EtudiantInfos.Parents)
        {
            var parent = await db.Parents.FirstOrDefaultAsync(p => p.NumeroTelphone == parentDto.NumeroTelephone, ct);

            if (parent is null)
            {
                parent = parentDto.Adapt<ParentEntity>();
                parent.NumeroTelphone = parentDto.NumeroTelephone;
            }

            var lien = new EtudiantParentEntity
            {
                Role = parentDto.Role,
                Parent = parent,
                Etudiant = etudiant
            };

            etudiant.EtudiantParents.Add(lien);
        }

        db.Etudiants.Add(etudiant);

        var inscription = new InscriptionEntity
        {
            Etudiant = etudiant,
            FiliereNiveau = classe,
            AnneeAcademique = annee,
            FiliereNiveauId = request.IdFiliereNiveau,
            AnneeAcademiqueId = request.IdAnneeAcademique,
            TypeInscription = TypeInscription.PremiereInscription,
            Statut = StatutInscription.Inscrit
        };

        // --- Génération de l'échéancier si des plans de paiement sont choisis ---
        if (request.PlansPaiementIds != null && request.PlansPaiementIds.Count > 0)
        {
            var distinctPlanIds = request.PlansPaiementIds.Distinct().ToList();
            var plans = await db.PlansPaiement
                .Include(p => p.FraisCatalogue)
                .Include(p => p.ModelesTranches)
                .Where(p => distinctPlanIds.Contains(p.Id))
                .ToListAsync(ct);

            if (plans.Count != distinctPlanIds.Count)
            {
                throw new NotFoundException("Un ou plusieurs plans de paiement spécifiés n'existent pas.");
            }

            foreach (var plan in plans)
            {
                if (!plan.EstActif)
                {
                    throw new BadRequestException($"Le plan de paiement '{plan.Nom}' est inactif.");
                }

                if (plan.FraisCatalogue.AnneeAcademiqueId != request.IdAnneeAcademique)
                {
                    throw new BadRequestException($"Le plan de paiement '{plan.Nom}' n'appartient pas à l'année académique de l'inscription.");
                }

                // Vérification du ciblage du frais (Cycle, Filière, Niveau)
                var frais = plan.FraisCatalogue;
                bool correspondCycle = !frais.CycleId.HasValue || (classe.Niveau.CycleId.HasValue && frais.CycleId == classe.Niveau.CycleId);
                bool correspondFiliere = !frais.FiliereId.HasValue || frais.FiliereId == classe.FiliereId;
                bool correspondNiveau = !frais.NiveauId.HasValue || frais.NiveauId == classe.NiveauId;

                if (!correspondCycle || !correspondFiliere || !correspondNiveau)
                {
                    throw new BadRequestException($"Le plan de paiement '{plan.Nom}' est associé à un frais qui ne s'applique pas à la classe '{classe.LibelleAffiche ?? classe.Niveau.Code}'.");
                }

                // Génération des échéances réelles pour l'étudiant
                foreach (var tranche in plan.ModelesTranches.OrderBy(t => t.NumeroOrdre))
                {
                    var echeance = new EcheanceEtudiantEntity
                    {
                        Inscription = inscription,
                        FraisCatalogueId = plan.FraisCatalogueId,
                        NumeroOrdre = tranche.NumeroOrdre,
                        Libelle = tranche.Libelle,
                        MontantAttendu = tranche.Montant,
                        MontantDejaPaye = 0m,
                        DateLimite = tranche.DateLimite,
                        ExigePourValiderInscription = tranche.ExigePourValiderInscription,
                        Statut = StatutEcheance.NonPaye
                    };
                    inscription.Echeances.Add(echeance);
                }
            }

            bool exigePaiement = inscription.Echeances.Any(e => e.ExigePourValiderInscription);
            inscription.Statut = exigePaiement ? StatutInscription.EnAttentePaiement : StatutInscription.Validee;
        }

        db.Inscriptions.Add(inscription);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            var vraiMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            throw new ConflictException($"Conflit technique : {vraiMessage}");
        }

        inscription.Etudiant = etudiant;
        inscription.FiliereNiveau = classe;
        inscription.AnneeAcademique = annee;

        return inscription.Adapt<RegisterInscriptionResponse>();
    }
}
