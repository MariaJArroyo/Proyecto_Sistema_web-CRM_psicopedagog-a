namespace MenteActiva.Seguridad;

// Datos que van dentro de la cookie de sesion, ademas del nombre y el rol.
public static class ClaimsMenteActiva
{
    // El token que el API entrego al iniciar sesion. Viaja dentro de la cookie,
    // que va cifrada, asi que el navegador no puede leerlo.
    public const string TokenApi = "TokenApi";

    // Solo lo llevan los usuarios del portal. Con el se sabe que estudiantes
    // puede ver esa persona.
    public const string IdEncargado = "IdEncargado";
}
