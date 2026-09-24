namespace SistemaPsicopedagogia.Api.Data;

public static class CrmData
{
    public static readonly object Dashboard = new
    {
        clientesActivos = 8,
        estudiantesActivos = 7,
        citasSemana = 6,
        pagosPendientes = 3,
        proximasCitas = new[]
        {
            new { fecha = "Lunes 13 de julio, 2026", hora = "9:00 a. m.", estudiante = "María Rodríguez", tipo = "Tutoría académica", estado = "Confirmada" },
            new { fecha = "Lunes 13 de julio, 2026", hora = "2:00 p. m.", estudiante = "Diego Vargas", tipo = "Sesión psicopedagógica", estado = "Programada" },
            new { fecha = "Martes 14 de julio, 2026", hora = "10:00 a. m.", estudiante = "Ana Castro", tipo = "Evaluación", estado = "Programada" }
        }
    };

    public static readonly object[] Clientes =
    {
        new { id = 1, encargado = "Carlos Jiménez", telefono = "8888-1111", correo = "carlos.jimenez@example.com", servicio = "Tutoría académica", estudiante = "María Rodríguez", estado = "Activo" },
        new { id = 2, encargado = "Rodolfo Vargas", telefono = "8888-2222", correo = "rodolfo.vargas@example.com", servicio = "Apoyo psicopedagógico", estudiante = "Diego Vargas", estado = "Cita agendada" },
        new { id = 3, encargado = "Marjorie Castro", telefono = "8888-3333", correo = "marjorie.castro@example.com", servicio = "Evaluaciones", estudiante = "Ana Castro", estado = "Contactado" },
        new { id = 4, encargado = "José Fernández", telefono = "8888-4444", correo = "jose.fernandez@example.com", servicio = "Orientación a padres", estudiante = "Kevin Fernández", estado = "Activo" }
    };

    public static readonly object[] Estudiantes =
    {
        new { id = 1, nombre = "María Rodríguez", encargado = "Carlos Jiménez", nivel = "Primaria", servicio = "Tutoría académica", estado = "Activo" },
        new { id = 2, nombre = "Diego Vargas", encargado = "Rodolfo Vargas", nivel = "Secundaria", servicio = "Apoyo psicopedagógico", estado = "Activo" },
        new { id = 3, nombre = "Ana Castro", encargado = "Marjorie Castro", nivel = "Primaria", servicio = "Evaluación", estado = "Activo" },
        new { id = 4, nombre = "Kevin Fernández", encargado = "José Fernández", nivel = "Secundaria", servicio = "Tutoría académica", estado = "Activo" }
    };

    public static readonly object[] Agenda =
    {
        new { id = 1, fecha = "13/07/2026", hora = "9:00 a. m.", estudiante = "María Rodríguez", encargado = "Carlos Jiménez", tipo = "Tutoría académica", modalidad = "Presencial", estado = "Confirmada" },
        new { id = 2, fecha = "13/07/2026", hora = "2:00 p. m.", estudiante = "Diego Vargas", encargado = "Rodolfo Vargas", tipo = "Sesión psicopedagógica", modalidad = "Virtual", estado = "Programada" },
        new { id = 3, fecha = "14/07/2026", hora = "10:00 a. m.", estudiante = "Ana Castro", encargado = "Marjorie Castro", tipo = "Evaluación", modalidad = "Presencial", estado = "Programada" },
        new { id = 4, fecha = "15/07/2026", hora = "3:30 p. m.", estudiante = "Kevin Fernández", encargado = "José Fernández", tipo = "Reunión con padres", modalidad = "Presencial", estado = "Confirmada" },
        new { id = 5, fecha = "16/07/2026", hora = "11:00 a. m.", estudiante = "Valeria Mora", encargado = "Marta Mora", tipo = "Tutoría académica", modalidad = "Presencial", estado = "Completada" }
    };

    public static readonly object[] Pagos =
    {
        new { id = 1, fecha = "10/07/2026", estudiante = "María Rodríguez", concepto = "Tutoría académica", monto = 25000, estado = "Pagado" },
        new { id = 2, fecha = "11/07/2026", estudiante = "Diego Vargas", concepto = "Sesión psicopedagógica", monto = 30000, estado = "Pendiente" },
        new { id = 3, fecha = "12/07/2026", estudiante = "Ana Castro", concepto = "Evaluación", monto = 35000, estado = "Pendiente" }
    };

    public static readonly object[] Planes = Array.Empty<object>();
    public static readonly object[] Sesiones = Array.Empty<object>();
    public static readonly object[] Materiales = Array.Empty<object>();
    public static readonly object[] Usuarios = Array.Empty<object>();
    public static readonly object[] UsuariosExternos = Array.Empty<object>();
    public static readonly object[] Comunicacion = Array.Empty<object>();
    public static readonly object[] Reportes = Array.Empty<object>();
    public static readonly object[] Portal = Array.Empty<object>();
}
