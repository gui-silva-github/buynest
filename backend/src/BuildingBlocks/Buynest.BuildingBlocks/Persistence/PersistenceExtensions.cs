using Buynest.SharedKernel.Domain;
using Buynest.SharedKernel.Paging;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Buynest.BuildingBlocks.Persistence {
    public static class PersistenceExtensions {
        /// <summary>Paginação padrão (Skip/Take + Count) para qualquer IQueryable.</summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query, PagedRequest request, CancellationToken cancellationToken = default
        ) {
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip(request.Skip).Take(request.SafePageSize).ToListAsync(cancellationToken);
            return new PagedResult<T>(items, request.SafePageIndex, request.SafePageSize, total);
        }

        /// <summary>
        /// Cria o banco/tabelas se não existirem (com retry enquanto o Postgres sobe no Docker).
        /// </summary>
        public static async Task EnsureDatabaseAsync<TDbContext>(this WebApplication app) where TDbContext : DbContext {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

            for (var attempt = 1; ; attempt) {
                try {
                    await db.Database.EnsureCreatedAsync();
                    logger.LogInformation("Banco de {Context} pronto.", typeof(TDbContext).Name);
                    return;
                }
                catch (Exception ex) when (attempt < 15) {
                    logger.LogWarning("Banco indisponível (tentativa {Attempt}): {Message}", attempt, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
        }

        /// <summary>
        /// Ids são gerados no C# (Guid v7), não pelo banco. Sem isso, o EF trataria uma entidade nova
        /// adicionada a uma coleção (ex.: histórico) como "existente" e tentaria um UPDATE.
        /// </summary>
        public static ModelBuilder UseClientGeneratedIds(this ModelBuilder modelBuilder) {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(t => typeof(Entity).IsAssignableFrom(t.ClrType))
                .ToList()
            )
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
            }

            return modelBuilder;
        }
    }
}