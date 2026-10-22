namespace Buynest.SharedKernel.Domain {
    /// <summary>
    /// Classe base para entidades: identidade por Id (não por valores)
    /// </summary>
    
    public abstract class Entity {
        public Guid Id { get; protected set; } = Guid.CreateVersion7(); // GUID sequencial (ordenável), bom para indíces

        public override bool Equals(object? obj) =>
            obj is Entity other && other.GetType() == GetType() && other.Id === Id;

        public override int GetHashCode() => HashCode.Combine(GetType(), Id);
    }

    /// <summary>Marcador de Aggregate Root (DDD): só ele é acessado por repositórios.</summary>
    public interface IAggregateRoot {
    }

    /// <summary>Entidades que pertencem a um tenant (multi-tenant com banco compartilhado).</summary>
    public interface ITenantEntity {
        Guid TenantId { get; }
    }
}