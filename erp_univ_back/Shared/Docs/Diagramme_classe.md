# Diagramme de Classe — ERP Universités Privées (MVP)

> **Note importante** : `id` est l'identifiant technique interne (clé primaire, généré par le système, jamais affiché). `matricule` est l'identifiant métier (visible, utilisé sur les documents officiels, saisi ou généré selon une règle propre à l'université). Ces deux champs sont **distincts** pour `Etudiant` et `Professeur`.

> **Changement d'architecture** : le système utilise désormais une base de données partagée entre toutes les universités (plutôt qu'une base dédiée par université). Chaque table métier porte un champ `etablissementId`, appliqué automatiquement par la couche d'accès aux données pour garantir l'isolation logique des données entre établissements.

```mermaid
classDiagram

    class Etablissement {
        +UUID id
        +String nom
        +String adresse
        +String telephone
        +String email
        +String logo
        +String slug
        +Enum statut
        +DateTime dateCreation
    }

    class Utilisateur {
        +UUID etablissementId
        +UUID id
        +String email
        +String motDePasseHash
        +String nom
        +String prenom
        +Enum role
        +Boolean actif
        +DateTime dateCreation
        +UUID professeurId
        +UUID etudiantId
    }

    class Etudiant {
        +UUID id
        +UUID etablissementId
        +String matricule
        +String nom
        +String prenom
        +Date dateNaissance
        +String lieuNaissance
        +String numeroCin
        +String email
        +String telephone
        +String profession
        +String employeur
        +String nomPere
        +String professionPere
        +Boolean pereDecede
        +String nomMere
        +String professionMere
        +Boolean mereDecedee
        +String nomTuteur
        +String telephoneContact
        +Enum statut
        +String niveau
        +String cycle
        +UUID filiereId
    }

    class Candidature {
        +UUID id
        +String nom
        +String prenom
        +String contact
        +UUID filiereSouhaiteeId
        +Date dateConcours
        +Enum resultat
    }

    class Professeur {
        +UUID id
        +UUID etablissementId
        +String matricule
        +String nom
        +String prenom
        +Date dateNaissance
        +String lieuNaissance
        +String cin
        +String telephone
        +String email
        +Decimal tauxHoraire
        +Enum statut
    }

    class Filiere {
        +UUID id
        +UUID etablissementId
        +String nom
    }

    class Matiere {
        +UUID id
        +UUID etablissementId
        +String nom
    }

    class Cours {
        +UUID id
        +UUID matiereId
        +UUID filiereId
        +String niveau
        +String anneeAcademique
        +UUID professeurId
        +Decimal coefficient
    }

    class Note {
        +UUID id
        +UUID etudiantId
        +UUID coursId
        +Enum typeEvaluation
        +Decimal valeur
        +Decimal coefficientUtilise
        +DateTime dateSaisie
        +UUID saisiParId
    }

    class Inscription {
        +UUID id
        +UUID etudiantId
        +UUID candidatureId
        +UUID filiereId
        +String anneeAcademique
        +Enum type
        +Enum statut
    }

    class TypeFrais {
        +UUID id
        +String nom
        +Decimal montantStandard
    }

    class PlanPaiement {
        +UUID id
        +UUID filiereId
        +String anneeAcademique
        +Enum frequence
        +Integer nombreEcheances
        +Decimal tauxReductionUnique
    }

    class ModeleEcheance {
        +UUID id
        +UUID planPaiementId
        +Integer ordre
        +String libelle
        +Decimal montantOuPourcentage
        +String dateLimiteRelative
        +String semestreConcerne
    }

    class Frais {
        +UUID id
        +UUID typeFraisId
        +UUID inscriptionId
        +UUID candidatureId
        +UUID coursId
        +UUID modeleEcheanceId
        +Decimal montantDu
        +Decimal montantPaye
        +Enum statut
    }

    class Paiement {
        +UUID id
        +UUID fraisId
        +Decimal montant
        +Enum mode
        +String referenceTransaction
        +DateTime datePaiement
        +Enum statut
        +UUID saisiParId
    }

    class PresenceEtudiant {
        +UUID id
        +UUID etudiantId
        +UUID coursId
        +Date date
        +Enum statut
        +UUID saisiParId
    }

    class ConfigAssiduite {
        +UUID id
        +UUID filiereId
        +Integer seuilAbsencesSanction
        +Enum typeSanction
    }

    class SeancePresence {
        +UUID id
        +UUID professeurId
        +UUID coursId
        +Date date
        +Time heureDebut
        +Time heureFin
        +Integer dureeMinutes
        +Enum modeSaisie
        +UUID saisiParId
        +Enum statut
    }

    class ConfigHonoraire {
        +UUID id
        +UUID filiereId
        +Decimal seuilHeuresNormales
        +Decimal tauxMajoration
    }

    class Honoraire {
        +UUID id
        +UUID professeurId
        +Date periodeDebut
        +Date periodeFin
        +Decimal heuresNormales
        +Decimal heuresSupplementaires
        +Decimal montantNormal
        +Decimal montantSupplementaire
        +Decimal montantTotal
        +Enum statut
        +UUID valideParId
    }

    class JournalAudit {
        +UUID id
        +UUID utilisateurId
        +String action
        +String tableConcernee
        +UUID enregistrementId
        +String details
        +DateTime horodatage
    }

    %% Relations — Multi-tenant
    Etablissement "1" --> "0..*" Utilisateur : compte
    Etablissement "1" --> "0..*" Etudiant : inscrit
    Etablissement "1" --> "0..*" Professeur : emploie
    Etablissement "1" --> "0..*" Filiere : propose
    Etablissement "1" --> "0..*" Matiere : propose

    %% Relations — Comptes
    Utilisateur "0..1" --> "1" Professeur : lié à
    Utilisateur "0..1" --> "1" Etudiant : lié à
    Utilisateur "1" --> "0..*" JournalAudit : génère

    %% Relations — Inscription
    Candidature "0..1" --> "1" Filiere : postule pour
    Candidature "0..1" --> "1" Etudiant : devient (si admis)
    Etudiant "1" --> "0..*" Inscription : effectue
    Inscription "0..*" --> "1" Filiere : concerne
    Inscription "0..1" --> "0..1" Candidature : issue de

    %% Relations — Programme pédagogique
    Matiere "1" --> "0..*" Cours : enseignée via
    Filiere "1" --> "0..*" Cours : propose
    Professeur "1" --> "0..*" Cours : enseigne
    Cours "1" --> "0..*" Note : concerne
    Cours "1" --> "0..*" PresenceEtudiant : suivi via
    Cours "1" --> "0..*" SeancePresence : donne lieu à
    Etudiant "1" --> "0..*" Note : obtient
    Etudiant "1" --> "0..*" PresenceEtudiant : assiste à

    %% Relations — Finance
    Filiere "1" --> "0..*" PlanPaiement : configure
    PlanPaiement "1" --> "1..*" ModeleEcheance : définit
    ModeleEcheance "1" --> "0..*" Frais : génère
    TypeFrais "1" --> "0..*" Frais : catégorise
    Inscription "0..1" --> "0..*" Frais : génère
    Candidature "0..1" --> "0..*" Frais : génère
    Cours "0..1" --> "0..*" Frais : concerne (rattrapage)
    Frais "1" --> "0..*" Paiement : reçoit

    %% Relations — Assiduité et honoraires
    Filiere "1" --> "0..1" ConfigAssiduite : paramètre
    Filiere "1" --> "0..1" ConfigHonoraire : paramètre
    Professeur "1" --> "0..*" Honoraire : perçoit
    SeancePresence "0..*" --> "1" Honoraire : agrégée dans
```

---

## Dictionnaire rapide — champs `id` vs `matricule`

| Entité         | `id` (technique)                                                            | `matricule` (métier)                                                                                                                                      |
| -------------- | --------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Etudiant**   | UUID interne, jamais affiché, utilisé uniquement pour les relations en base | Identifiant lisible affiché sur les bulletins, cartes, documents officiels (ex: `ETU-2026-0347`) — propre au système de numérotation de chaque université |
| **Professeur** | UUID interne, jamais affiché                                                | Identifiant lisible affiché sur les fiches de paie/honoraires (ex: `PROF-2026-012`)                                                                       |

Ces deux champs ne sont **jamais confondus** dans les relations entre tables : toutes les clés étrangères (`etudiantId`, `professeurId`) pointent vers le champ `id`, jamais vers le `matricule`. Le `matricule` reste un attribut d'affichage, éventuellement modifiable en cas d'erreur de saisie, sans casser aucune relation en base.

---

_Diagramme généré à partir du modèle consolidé de la conversation, reflétant les simplifications MVP actées dans le document des règles de gestion._
1. Le module Financier (Scolarité et Paiements)
Vous avez la table Inscriptions, mais l'inscription doit être validée par la caisse.

Entités à créer : TypeFrais (droit d'inscription, écolage), Tranche (échéancier), et Paiement (la transaction).

Objectif : Suivre les encaissements, gérer les tranches impayées et valider définitivement le statut de l'étudiant.

2. L'Affectation Pédagogique (Professeur ↔ Matière)
Vous avez la table Professeurs et la table Matieres, mais aucune relation entre les deux.

Entités à créer : Affectation ou ProfesseurMatiere (table de jonction).

Objectif : Assigner un enseignant à une matière pour une année académique donnée. C'est indispensable pour préparer les emplois du temps et calculer les heures à payer aux professeurs (vacations).

3. Le module d'Évaluation (Examens et Notes)
Vous avez le programme (Maquette) et les étudiants, il faut maintenant les évaluer.

Entités à créer : Examen (ou Session d'examen) et Note.

Objectif : Lier un étudiant, une matière et un score. C'est la base pour générer les relevés de notes et valider les passages en année supérieure.

4. Le module de lecture complexe (Maquette Pédagogique)
C'est un module VSA purement "Read Model". Vous n'avez aucune table à créer ici.

Action : Développer le cas d'usage GetMaquetteComplete.

Objectif : Utiliser .AsSplitQuery() pour récupérer la chaîne complète Filière ➔ Niveau ➔ UE ➔ Matière et l'envoyer au frontend en un seul JSON structuré pour l'affichage du catalogue.