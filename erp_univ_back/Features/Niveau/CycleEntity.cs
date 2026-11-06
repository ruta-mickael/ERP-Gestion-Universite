using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp_univ_back.Features.ConfigFinancier;
using erp_univ_back.Shared.Entities;

namespace erp_univ_back.Features.Niveau;

[Table("cycles")]
public class CycleEntity : BaseEntity
{
    // Identifiants constants pour les cycles prédéfinis LMD
    public static readonly Guid LicenceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid MasterId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DoctoratId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private string _code = string.Empty;

    [Required]
    [MaxLength(20)]
    public required string Code
    {
        get => _code;
        set => _code = value.Trim().ToUpperInvariant();
    }

    [Required]
    [MaxLength(100)]
    public required string Nom { get; set; }

    public int Ordre { get; set; }

    public ICollection<NiveauEntity> Niveaux { get; set; } = [];
    public ICollection<FraisCatalogueEntity> FraisCatalogues { get; set; } = [];
}

