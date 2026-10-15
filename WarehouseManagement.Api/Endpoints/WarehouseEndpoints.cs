
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Api.Contracts;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Validation;

namespace WarehouseManagement.Api.Endpoints;

public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/warehouses", async (
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

            var query = db.Warehouses.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(w =>
                    w.WarehouseCode.Contains(keyword) ||
                    w.WarehouseName.Contains(keyword));
            }

            if (isActive.HasValue)
            {
                query = query.Where(w => w.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var warehouses = await query
                .OrderBy(w => w.WarehouseCode)
                .ThenBy(w => w.WarehouseId)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .Select(w => new
                {
                    w.WarehouseId,
                    w.WarehouseCode,
                    w.WarehouseName,
                    w.IsActive
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                page = currentPage,
                pageSize = currentPageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(
                    totalCount / (double)currentPageSize),
                items = warehouses
            });
        });


        app.MapGet("/api/warehouses/{id:int}", async (
            int id,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Warehouse ID must be greater than zero."
                });
            }

            var warehouse = await db.Warehouses
                .AsNoTracking()
                .Where(w => w.WarehouseId == id)
                .Select(w => new
                {
                    w.WarehouseId,
                    w.WarehouseCode,
                    w.WarehouseName,
                    w.IsActive,
                    w.CreatedAt,
                    w.UpdatedAt
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (warehouse is null)
            {
                return Results.NotFound(new
                {
                    message = $"Warehouse with ID {id} was not found."
                });
            }

            return Results.Ok(warehouse);
        });


        app.MapPost("/api/warehouses", async (
            CreateWarehouseRequest request,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            // 1. Thay thế phần kiểm tra chuỗi bằng StringValidation.IsRequiredTextValid
            if (!StringValidation.IsRequiredTextValid(request.WarehouseCode, 50) ||
                !StringValidation.IsRequiredTextValid(request.WarehouseName, 200))
            {
                return Results.BadRequest(new
                {
                    message = "WarehouseCode and WarehouseName are required and must not exceed their maximum lengths."
                });
            }

            // 2. Giữ nguyên phần Trim()
            var warehouseCode = request.WarehouseCode.Trim();
            var warehouseName = request.WarehouseName.Trim();

            // 3. Giữ nguyên phần kiểm tra mã kho trùng
            var codeExists = await db.Warehouses
                .AnyAsync(
                    w => w.WarehouseCode == warehouseCode,
                    cancellationToken);

            if (codeExists)
            {
                return Results.Conflict(new
                {
                    message = "WarehouseCode already exists."
                });
            }

            // 4. Giữ nguyên phần lưu dữ liệu
            var now = DateTime.UtcNow;

            var warehouse = new Warehouse
            {
                WarehouseCode = warehouseCode,
                WarehouseName = warehouseName,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/api/warehouses/{warehouse.WarehouseId}",
                new
                {
                    warehouse.WarehouseId,
                    warehouse.WarehouseCode,
                    warehouse.WarehouseName,
                    warehouse.IsActive,
                    warehouse.CreatedAt,
                    warehouse.UpdatedAt
                });
        });



        app.MapPut("/api/warehouses/{id:int}", async (
            int id,
            UpdateWarehouseRequest request,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Warehouse ID must be greater than zero."
                });
            }

            var warehouseName = request.WarehouseName.Trim();

            if (string.IsNullOrWhiteSpace(warehouseName))
            {
                return Results.BadRequest(new
                {
                    message = "WarehouseName is required."
                });
            }

            var warehouse = await db.Warehouses
                .SingleOrDefaultAsync(
                    w => w.WarehouseId == id,
                    cancellationToken);

            if (warehouse is null)
            {
                return Results.NotFound(new
                {
                    message = $"Warehouse with ID {id} was not found."
                });
            }

            warehouse.WarehouseName = warehouseName;
            warehouse.IsActive = request.IsActive;
            warehouse.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new
            {
                warehouse.WarehouseId,
                warehouse.WarehouseCode,
                warehouse.WarehouseName,
                warehouse.IsActive,
                warehouse.CreatedAt,
                warehouse.UpdatedAt
            });
        });


        return app;
    }
}
