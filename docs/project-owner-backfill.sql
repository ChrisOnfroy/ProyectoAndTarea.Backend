-- La migración deja OwnerId en NULL para los proyectos que ya existían.
-- Esos proyectos no aparecen en la API hasta que se les asigne un usuario.
-- Primero localiza el usuario que debe recibirlos:
-- SELECT "Id", "Email" FROM "AspNetUsers" ORDER BY "Email";
--
-- Sustituye el UUID del ejemplo por el Id real. Ejecuta y confirma este UPDATE
-- por grupos de proyectos cuya propiedad conozcas; no lo ejecutes a ciegas.
BEGIN;

UPDATE "Proyectos"
SET "OwnerId" = 'UUID-REAL-DEL-USUARIO'::uuid
WHERE "OwnerId" IS NULL
  AND "Id" IN (
      'UUID-REAL-DEL-PROYECTO-1'::uuid,
      'UUID-REAL-DEL-PROYECTO-2'::uuid
  );

-- Comprueba el resultado antes de confirmar la transacción.
SELECT "Id", "Name", "OwnerId"
FROM "Proyectos"
WHERE "OwnerId" IS NULL
   OR "OwnerId" = 'UUID-REAL-DEL-USUARIO'::uuid;

-- Si el resultado es correcto, cambia ROLLBACK por COMMIT.
ROLLBACK;
