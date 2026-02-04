# TeamLunch
> **Decisiones en equipo, democráticas y en tiempo real.**

**TeamLunch** es una plataforma moderna de votación diseñada para simplificar la toma de decisiones grupales. Aunque nació para resolver el eterno debate de *"¿Qué comemos?"*, su arquitectura flexible permite decidir sobre **cualquier tema**:

*   ¿Qué película ver hoy?
*   ¿Nombre para el próximo Sprint/Proyecto?
*   ¿A qué jugamos el viernes?
*    Y sí... ¿Qué pedimos de comer?

Construida con lo último del ecosistema **.NET**, ofrece una experiencia de usuario fluida, instantánea y visualmente atractiva.

## ✨ Características Destacadas

### ⚡ Votación en Tiempo Real (Live)
*   **WebSockets (SignalR):** Sin recargas. Mira cómo las barras de votación se mueven al instante cuando alguien vota.
*   **Resultados en Vivo:** Visualiza quién va ganando con animaciones fluidas y conteo dinámico.
*   **Temporizador Global:** Configura un límite de tiempo para forzar una decisión rápida.

### 💬 Sala de Chat Multimedia
*   **Comunicación Integrada:** Discute las opciones sin salir de la votación.
*   **Soporte de Audio:** Envía notas de voz nativas desde el navegador.
*   **Imágenes via URL:** Comparte menús, pósters de películas o memes.
*   **Indicadores de Escritura:** Feedback visual cuando alguien está escribiendo ("Juan está escribiendo...").

### �️ Funcionalidades "Pro"
*   **Persistencia Inteligente:** ¿Cerraste la pestaña por error? Tu sesión se recupera automáticamente.
*   **Modo Administrador:** Solo el creador de la sala tiene el poder de "Finalizar Votación" para evitar cierres prematuros.
*   **Temas (Dark/Light):** Interfaz adaptable con un elegante Modo Oscuro para sesiones nocturnas.
*   **Sistema Anti-Empates:** Lógica inteligente para declarar ganadores o detectar empates automáticamente.

## 🛠️ Stack Tecnológico

Este proyecto demuestra una arquitectura moderna y escalable utilizando 100% .NET:

*   **Frontend:** [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) (C# en el navegador)
*   **Backend:** ASP.NET Core
*   **Tiempo Real:** [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr)
*   **UI/UX:** [MudBlazor](https://mudblazor.com/) (Material Design Components)
*   **Audio/Storage:** JavaScript Interop

## Cómo Empezar

1.  **Clonar el repositorio**
    ```bash
    git clone https://github.com/tu-usuario/TeamLunch.git
    cd TeamLunch
    ```

2.  **Ejecutar el servidor**
    ```bash
    cd TeamLunch.Server
    dotnet run
    ```

3.  **Abrir en el navegador**
    Ingresa a `https://localhost:7148` (o el puerto indicado) y ¡crea tu primera sala!

