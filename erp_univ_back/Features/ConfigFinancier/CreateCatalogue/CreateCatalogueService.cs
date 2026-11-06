namespace erp_univ_back.Features.ConfigFinancier.CreateCatalogue;

using erp_univ_back.Features.Filiere;
using erp_univ_back.Features.Niveau;
using erp_univ_back.Shared.Data;
using erp_univ_back.Shared.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public interface ICreateCatalogueService
{
    Task<CreateCatalogueResponse> ExecuteAsync(CreateCatalogueRequest request, CancellationToken ct = default);
}

public class CreateCatalogueService(
    ErpUnivDbContext db,
    IValidator<CreateCatalogueRequest> validator) : ICreateCatalogueService
{
    public async Task<CreateCatalogueResponse> ExecuteAsync(CreateCatalogueRequest request, CancellationToken ct = default)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.Errors.First().ErrorMessage);
        }

        // 1. Vérification de l'année académique
        var anneeAcademique = await db.AnneeAcademiques
            .FirstOrDefaultAsync(a => a.Id == request.AnneeAcademiqueId, ct)
            ?? throw new NotFoundException($"L'année académique avec l'identifiant '{request.AnneeAcademiqueId}' n'existe pas.");

        // 2. Vérification optionnelle du Cycle
        CycleEntity? cycle = null;
        if (request.CycleId.HasValue)
        {
            cycle = await db.Cycles
                .FirstOrDefaultAsync(c => c.Id == request.CycleId.Value, ct)
                ?? throw new NotFoundException($"Le cycle avec l'identifiant '{request.CycleId.Value}' n'existe pas.");
        }

        // 3. Vérification optionnelle de la Filière
        FiliereEntity? filiere = null;
        if (request.FiliereId.HasValue)
        {
            filiere = await db.Filieres
                .FirstOrDefaultAsync(f => f.Id == request.FiliereId.Value, ct)
                ?? throw new NotFoundException($"La filière avec l'identifiant '{request.FiliereId.Value}' n'existe pas.");
        }

        // 4. Vérification optionnelle du Niveau
        NiveauEntity? niveau = null;
        if (request.NiveauId.HasValue)
        {
            niveau = await db.Niveaux
                .FirstOrDefaultAsync(n => n.Id == request.NiveauId.Value, ct)
                ?? throw new NotFoundException($"Le niveau avec l'identifiant '{request.NiveauId.Value}' n'existe pas.");
        }

        // 5. Si FiliereId ET NiveauId sont fournis, on s'assure qu'une classe les associe bien dans l'université
        if (request.FiliereId.HasValue && request.NiveauId.HasValue)
        {
            var classeExiste = await db.FiliereNiveaux
                .AnyAsync(fn => fn.FiliereId == request.FiliereId.Value && fn.NiveauId == request.NiveauId.Value, ct);

            if (!classeExiste)
            {
                throw new NotFoundException($"Aucune classe n'associe la filière '{filiere?.Nom}' et le niveau '{niveau?.Nom}'.");
            }
        }

        // 6. Vérification des doublons
        var libelleNormalise = request.Libelle.Trim().ToLowerInvariant();
        var existe = await db.FraisCatalogues.AnyAsync(f =>
            f.AnneeAcademiqueId == request.AnneeAcademiqueId &&
            f.CycleId == request.CycleId &&
            f.FiliereId == request.FiliereId &&
            f.NiveauId == request.NiveauId &&
            f.Categorie == request.Categorie &&
            f.Libelle.ToLowerInvariant() == libelleNormalise,
            ct);

        if (existe)
        {
            throw new ConflictException($"Un frais de catalogue nommé '{request.Libelle.Trim()}' existe déjà pour cette configuration et cette année académique.");
        }

        // 7. Création de l'entité
        var entity = new FraisCatalogueEntity
        {
            AnneeAcademiqueId = request.AnneeAcademiqueId,
            CycleId = request.CycleId,
            FiliereId = request.FiliereId,
            NiveauId = request.NiveauId,
            Categorie = request.Categorie,
            Libelle = request.Libelle.Trim(),
            MontantTotal = request.MontantTotal,
            MoyensPaiementAutorises = request.MoyensPaiementAutorises,
            EstObligatoirePourDossier = request.EstObligatoirePourDossier
        };

        db.FraisCatalogues.Add(entity);
        await db.SaveChangesAsync(ct);

        return new CreateCatalogueResponse(
            Id: entity.Id,
            AnneeAcademiqueId: entity.AnneeAcademiqueId,
            AnneeAcademiqueLibelle: anneeAcademique.Libelle,
            CycleId: entity.CycleId,
            CycleNom: cycle?.Nom,
            FiliereId: entity.FiliereId,
            FiliereNom: filiere?.Nom,
            NiveauId: entity.NiveauId,
            NiveauNom: niveau?.Nom,
            Categorie: entity.Categorie,
            Libelle: entity.Libelle,
            MontantTotal: entity.MontantTotal,
            MoyensPaiementAutorises: entity.MoyensPaiementAutorises,
            EstObligatoirePourDossier: entity.EstObligatoirePourDossier,
            DateCreation: entity.DateCreation
        );
    }
}
