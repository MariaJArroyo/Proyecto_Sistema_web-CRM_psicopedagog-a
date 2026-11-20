namespace MenteActiva.Models;

// Lo que lleva el encabezado de cada pantalla del portal: el icono de Bootstrap
// Icons (por ejemplo "bi-calendar-event"), el titulo y la linea de apoyo.
public record EncabezadoPortalViewModel(string Icono, string Titulo, string Subtitulo);
