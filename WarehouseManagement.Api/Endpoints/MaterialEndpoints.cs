
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Api.Contracts;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Validation;

namespace WarehouseManagement.Api.Endpoints;

public static class MaterialEndpoints
{
    public static IEndpointRouteBuilder MapMaterialEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/materials", async (
            WarehouseDbContext db,
            int? page,
            int? pageSize,
            string? search,
            bool? isActive,
            CancellationToken cancellationToken) =>
        {
            var currentPage = page ?? 1;
            var currentPageSize = pageSize ?? 10;

            if (currentPage < 1)
            {
                return Results.BadRequest(new
                {
                    message = "page must be greater than or equal to 1."
                });
            }

            if (currentPageSize < 1 || currentPageSize > 100)
            {
                return Results.BadRequest(new
                {
                    message = "pageSize must be between 1 and 100."
                });
            }

            var query = db.Materials.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(m =>
                    m.MaterialCode.Contains(keyword) ||
                    m.MaterialName.Contains(keyword));
            }

            if (isActive.HasValue)
            {
                query = query.Where(m => m.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var materials = await query
                .OrderBy(m => m.MaterialCode)
                .ThenBy(m => m.MaterialId)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .Select(m => new
                {
                    m.MaterialId,
                    m.MaterialCode,
                    m.MaterialName,
                    m.BaseUnit,
                    m.IsActive
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                page = currentPage,
                pageSize = currentPageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(
                    totalCount / (double)currentPageSize),
                items = materials
            });
        });


        app.MapGet("/api/materials/{id:int}", async (
            int id,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Material ID must be greater than zero."
                });
            }

            var material = await db.Materials
                .AsNoTracking()
                .Where(m => m.MaterialId == id)
                .Select(m => new
                {
                    m.MaterialId,
                    m.MaterialCode,
                    m.MaterialName,
                    m.BaseUnit,
                    m.IsActive,
                    m.CreatedAt,
                    m.UpdatedAt
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (material is null)
            {
                return Results.NotFound(new
                {
                    message = $"Material with ID {id} was not found."
                });
            }

            return Results.Ok(material);
        });


        app.MapPost("/api/materials", async (
            CreateMaterialRequest request,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!StringValidation.IsRequiredTextValid(request.MaterialCode, 50) ||
                !StringValidation.IsRequiredTextValid(request.MaterialName, 200) ||
                !StringValidation.IsRequiredTextValid(request.BaseUnit, 20))
            {
                return Results.BadRequest(new
                {
                    message = "MaterialCode, MaterialName and BaseUnit are required and must not exceed their maximum lengths."
                });
            }

            var materialCode = request.MaterialCode.Trim();
            var materialName = request.MaterialName.Trim();
            var baseUnit = request.BaseUnit.Trim();

            var codeExists = await db.Materials
                .AnyAsync(
                    m => m.MaterialCode == materialCode,
                    cancellationToken);

            if (codeExists)
            {
                return Results.Conflict(new
                {
                    message = "MaterialCode already exists."
                });
            }

            var now = DateTime.UtcNow;

            var material = new Material
            {
                MaterialCode = materialCode,
                MaterialName = materialName,
                BaseUnit = baseUnit,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Materials.Add(material);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/api/materials/{material.MaterialId}",
                new
                {
                    material.MaterialId,
                    material.MaterialCode,
                    material.MaterialName,
                    material.BaseUnit,
                    material.IsActive,
                    material.CreatedAt,
                    material.UpdatedAt
                });
        });


        app.MapPut("/api/materials/{id:int}", async (
            int id,
            UpdateMaterialRequest request,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Material ID must be greater than zero."
                });
            }

            var materialName = request.MaterialName.Trim();
            var baseUnit = request.BaseUnit.Trim();

            if (string.IsNullOrWhiteSpace(materialName) ||
                string.IsNullOrWhiteSpace(baseUnit))
            {
                return Results.BadRequest(new
                {
                    message = "MaterialName and BaseUnit are required."
                });
            }

            var material = await db.Materials
                .SingleOrDefaultAsync(
                    m => m.MaterialId == id,
                    cancellationToken);

            if (material is null)
            {
                return Results.NotFound(new
                {
                    message = $"Material with ID {id} was not found."
                });
            }

            material.MaterialName = materialName;
            material.BaseUnit = baseUnit;
            material.IsActive = request.IsActive;
            material.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new
            {
                material.MaterialId,
                material.MaterialCode,
                material.MaterialName,
                material.BaseUnit,
                material.IsActive,
                material.CreatedAt,
                material.UpdatedAt
            });
        });


        return app;
    }
}
