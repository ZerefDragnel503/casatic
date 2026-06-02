SELECT "NombreEmpresa", "Descripcion", "Especialidades", "Servicios"
FROM socios
WHERE "Slug" = 'bridge4-digital';

SELECT COUNT(*) AS malos
FROM socios
WHERE "Descripcion" LIKE '%??%'
   OR array_to_string("Especialidades", ',') LIKE '%??%'
   OR array_to_string("Servicios", ',') LIKE '%??%';

SELECT COUNT(*) AS socios
FROM socios;
