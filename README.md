# Mente Activa Psicopedagogía — Frontend

Maqueta funcional de interfaz (frontend estático) para un sistema web tipo CRM de
gestión de tutorías académicas y servicios de psicopedagogía. Proyecto pensado
para el consultorio unipersonal **Mente Activa Psicopedagogía** (Costa Rica),
dirigido por una psicopedagoga que atiende estudiantes de forma individual.

Proyecto académico de Ingeniería en Sistemas — Universidad Fidélitas.

## Estado actual del proyecto

Esta fase cubre **únicamente el frontend**: maquetación HTML, estilos y
comportamiento de interfaz en el navegador. No hay backend, base de datos ni
autenticación real:

- Los formularios no envían datos a ningún servidor; usan validación visual
  con JavaScript y muestran mensajes de confirmación simulados (toasts/alerts).
- Los datos que aparecen en tablas, calendarios y dashboards son datos de
  ejemplo (mock), escritos directamente en el HTML o en `Js/DatosMock.js`.
- El login es solo la pantalla de acceso; redirige a un panel según el tipo
  de usuario seleccionado, sin validar credenciales contra un servidor.

Todos los módulos definidos para esta etapa están construidos y son
navegables entre sí (ver [Módulos incluidos](#módulos-incluidos)).

## Tecnologías

- **HTML5 + CSS3 + JavaScript vanilla** — sin frameworks de frontend (React,
  Vue, Angular) ni herramientas de build.
- **Bootstrap 5** (vía CDN) como framework base de UI.
- **Bootstrap Icons** (vía CDN) para iconografía.
- **Google Fonts** — Poppins (títulos) e Inter (texto).
- Diseño **responsive** (móvil, tablet, escritorio).
- Interfaz completa en **español**.

El proyecto está preparado deliberadamente para una futura migración a
**ASP.NET Core MVC con Razor Views**: nombres de campos en PascalCase
alineados a futuros modelos C#, comentarios `<!-- Razor: ... -->` marcando
dónde irán bindings de modelo, y un layout compartido (sidebar/topbar) pensado
para convertirse en `_Layout.cshtml`.

## Estructura del proyecto

```
Frontend/
  Index.html                  Página pública (landing)
  Login.html                  Pantalla de acceso (admin / portal)
  RecuperarContrasena.html    Recuperación de contraseña (solo visual)

  Admin/                      Panel administrativo (uso interno)
    Dashboard.html
    Clientes.html
    Estudiantes.html
    EstudianteFicha.html
    Agenda.html
    Sesiones.html
    Planes.html
    Reportes.html
    Pagos.html
    Comunicacion.html
    Materiales.html
    Usuarios.html              Administración de usuarios internos
    UsuariosExternos.html      Administración de accesos del portal
    MiPerfil.html               Perfil del usuario interno autenticado

  Portal/                      Portal de padres / estudiantes
    Inicio.html
    Citas.html
    Progreso.html
    Tareas.html
    Pagos.html
    Perfil.html

  Css/
    Styles.css                 Design system y estilos globales

  Js/
    Main.js                    Sidebar, filtros, tablas buscables, modales, toasts
    Validaciones.js             Validaciones de formularios

  Img/
    LogoMenteActiva.jpeg        Logo del consultorio

Docs/
  DISENO.md                   Sistema de diseño (paleta, tipografía, componentes)
  PANTALLAS.md                 Detalle de contenido esperado por pantalla
  ANALISIS-HALLAZGOS.md        Auditoría de consistencia lógica del frontend
```

## Módulos incluidos

**Sitio público**
- Landing page con presentación del servicio, catálogo de servicios,
  testimonios, formulario de contacto y botón flotante de WhatsApp.
- Login con selector de tipo de acceso (personal administrativo / portal de
  familias) y recuperación de contraseña.

**Panel administrativo** (`Admin/`)
- Dashboard con resumen de estudiantes activos, citas de la semana, pagos
  pendientes y actividad reciente.
- CRM de clientes (encargados/leads) con búsqueda, filtros y estados.
- Gestión de estudiantes con ficha individual (datos académicos, áreas de
  dificultad, encargado asociado).
- Agenda de citas con vistas de día/semana/mes.
- Registro y seguimiento de sesiones.
- Planes de intervención (estrategias y actividades).
- Reportes de progreso por estudiante con filtros y exportación simulada.
- Registro de pagos con estados (pagado / pendiente / vencido).
- Comunicación: correo, mensajería interna y plantillas de mensajes.
- Materiales educativos: guías, tareas asignadas y enlaces útiles.
- Administración de usuarios internos y de usuarios externos (accesos del
  portal), más el perfil propio de cada usuario interno.

**Portal de padres/estudiantes** (`Portal/`)
- Resumen de bienvenida con selector de estudiante (para encargados con más
  de un estudiante registrado).
- Próximas citas, reportes de progreso, tareas asignadas y estado de pagos.
- Perfil del encargado con cambio de contraseña.

## Sistema de diseño

Definido en [`Docs/DISENO.md`](Docs/DISENO.md) e implementado como variables
CSS en `Frontend/Css/Styles.css`:

- Paleta: azul primario (confianza), verde menta (crecimiento/apoyo) y
  naranja suave (calidez) sobre fondos claros.
- Tipografía: Poppins para títulos, Inter para texto.
- Componentes reutilizables: tarjetas, badges de estado por dominio (clientes,
  citas, pagos), tablas con buscador, modales de confirmación para acciones
  destructivas y toasts de confirmación al guardar.

## Cómo verlo

Al ser un frontend 100% estático, no requiere instalación ni servidor: basta
con abrir cualquier archivo `.html` directamente en el navegador. El punto de
entrada natural es [`Frontend/Index.html`](Frontend/Index.html).

## Hecho por

- Jose Alejandro Lafuente Romero
- Maria jose Campos Arroyo
- Edgardo Antonio Solera Solano
- Sergio David Mata Lopez
