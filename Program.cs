using System;
using System.Collections.Generic;
using Spectre.Console;
using NAudio.Wave;
class Program
{
    static IWavePlayer waveOutDevice;
    static AudioFileReader audioFileReader;
   static string musicaFondoInicioPath = "D:/Vanci/Universidad/Proyecto/Assassin_s_Creed_2_OST___Jesper_Kyd_-_Ezio_s_Family__Track_03_(128k).mp3";
   static string musicaFondoJuegoPath = "D:/Vanci/Universidad/Proyecto/Uncontrollable_-_Xenoblade_Chronicles_X_OST(128k).m4a";
    static string[] jugadores = { "Jugador 1", "Jugador 2", "Jugador 3", "Jugador 4" };
    static int jugadorActual; 
    static int n = 33;
    static int centro = n / 2;
   
    static void Main(string[] args)
    {
       // Iniciar la música de fondo
        IniciarMusicaDeFondo(musicaFondoInicioPath);

        // Mostrar la pantalla de introducción
        MostrarPantallaDeIntroduccion();

        // Esperar a que el jugador presione una tecla para continuar
        AnsiConsole.Markup("[bold yellow]Presiona cualquier tecla para comenzar el juego...[/]\n");
        Console.ReadKey(true);

        char[,] tablero = GenerarTablero(n);

        // Establecer la meta en el centro del tablero
        tablero[centro, centro] = 'F';

        // Pedir al usuario la cantidad de jugadores
        var cantidadJugadores = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Seleccione la cantidad de [green]jugadores[/]:")
                .AddChoices(2, 3, 4));

        // Crear una lista de fichas disponibles para elegir
        List<Ficha> fichasDisponibles = CrearFichasDisponibles();

        List<Ficha> fichasJugador1 = new List<Ficha>();
        List<Ficha> fichasJugador2 = new List<Ficha>();
        List<Ficha> fichasJugador3 = new List<Ficha>();
        List<Ficha> fichasJugador4 = new List<Ficha>();

        // Selección de fichas para cada jugador
        for (int i = 1; i <= cantidadJugadores; i++)
        {
            AnsiConsole.Markup($"[bold yellow]Jugador {i}, seleccione una ficha:[/]\n");
            MostrarFichasDisponibles(fichasDisponibles);
            int eleccion = AnsiConsole.Prompt(
                new TextPrompt<int>("[green]Ingrese el número de la ficha seleccionada:[/]")
                .Validate(selection => selection > 0 && selection <= fichasDisponibles.Count ? ValidationResult.Success() : ValidationResult.Error("[red]Selección inválida[/]")));

            Ficha fichaElegida = fichasDisponibles[eleccion - 1];

            if (i == 1) fichasJugador1.Add(fichaElegida);
            else if (i == 2) fichasJugador2.Add(fichaElegida);
            else if (i == 3) fichasJugador3.Add(fichaElegida);
            else fichasJugador4.Add(fichaElegida);

            // Eliminar la ficha elegida de la lista de fichas disponibles
            fichasDisponibles.RemoveAt(eleccion - 1);
        }

        // Colocar fichas iniciales en el tablero
        ColocarFichasIniciales(tablero, fichasJugador1, 0, 0); // Esquina superior izquierda
        if (cantidadJugadores >= 2)
            ColocarFichasIniciales(tablero, fichasJugador2, 0, n - 1); // Esquina superior derecha
        if (cantidadJugadores >= 3)
            ColocarFichasIniciales(tablero, fichasJugador3, n - 1, 0); // Esquina inferior izquierda
        if (cantidadJugadores == 4)
            ColocarFichasIniciales(tablero, fichasJugador4, n - 1, n - 1); // Esquina inferior derecha
        
        // Detener la música de fondo cuando el juego termina
        DetenerMusicaDeFondo();
        // Imprimir el tablero inicial con las fichas
        Console.Clear();
        AnsiConsole.Markup("[bold yellow]Tablero inicial:[/]\n");
        ImprimirTablero(tablero);
        // Iniciar la música de fondo del juego
        IniciarMusicaDeFondo(musicaFondoJuegoPath);

        // Turnos de jugadores
        int turnoActual = 0;
        bool[] movimientosBloqueados = new bool[4];

        while (true) // Continuar hasta que alguien gane
        {
            jugadorActual = turnoActual % cantidadJugadores;
            AnsiConsole.Markup($"[bold yellow]{jugadores[jugadorActual]}, es tu turno:[/]\n");

            // Capturar la entrada del teclado
            List<Ficha> fichas = jugadorActual == 0 ? fichasJugador1 :
                                 jugadorActual == 1 ? fichasJugador2 :
                                 jugadorActual == 2 ? fichasJugador3 :
                                                      fichasJugador4;

            Ficha fichaAMover = fichas[0];

            // Manejar la velocidad de la ficha
            int movimientosRestantes = movimientosBloqueados[jugadorActual] ? 0 : fichaAMover.Velocidad;
                        movimientosBloqueados[jugadorActual] = false; // Restablecer el estado de movimiento bloqueado

            while (movimientosRestantes > 0)
    {
        AnsiConsole.Markup($"[bold yellow]Tienes {movimientosRestantes} movimientos restantes.[/]\n");
        var key = Console.ReadKey(true).Key;

        int nuevaFila = fichaAMover.PosicionFila;
        int nuevaColumna = fichaAMover.PosicionColumna;

        // Detectar la tecla 'Z' para usar la habilidad
        if (key == ConsoleKey.Z)
        {
            fichaAMover.UsarHabilidad(tablero);
            continue; // Saltar a la siguiente iteración después de usar la habilidad
        }

        // Decidir el nuevo movimiento basado en la tecla presionada
        switch (key)
        {
            case ConsoleKey.UpArrow:
                nuevaFila = fichaAMover.PosicionFila - 1;
                break;
            case ConsoleKey.DownArrow:
                nuevaFila = fichaAMover.PosicionFila + 1;
                break;
            case ConsoleKey.LeftArrow:
                nuevaColumna = fichaAMover.PosicionColumna - 1;
                break;
            case ConsoleKey.RightArrow:
                nuevaColumna = fichaAMover.PosicionColumna + 1;
                break;
            default:
                AnsiConsole.Markup("[red]Tecla no válida. Usa las flechas direccionales.[/]\n");
                continue; // Mantener el mismo turno si la tecla es inválida
        }

        // Verificar si el movimiento está fuera del tablero antes de mover la ficha
        if (nuevaFila < 0 || nuevaFila >= tablero.GetLength(0) || nuevaColumna < 0 || nuevaColumna >= tablero.GetLength(1))
        {
            AnsiConsole.Markup("[red]Movimiento inválido. Inténtalo de nuevo.[/]\n");
            continue; // Mantener el mismo turno si el movimiento es inválido
        }

        // Mover la ficha y verificar trampas
        bool movimientoValido = MoverFicha(tablero, fichaAMover, nuevaFila, nuevaColumna, ref movimientosBloqueados[jugadorActual], ref turnoActual);
        if (!movimientoValido)
        {
            continue; // Mantener el mismo turno si el movimiento fue inválido
        }

        movimientosRestantes--;

        // Verificar si el jugador ha llegado a la casilla del medio del mapa
        if (fichaAMover.PosicionFila == centro && fichaAMover.PosicionColumna == centro)
        {
            AnsiConsole.Markup($"[bold yellow]¡{jugadores[jugadorActual]} ha ganado![/]\n");
            return; // Termina el juego si un jugador ha ganado
        }

        // Imprimir el tablero después de cada movimiento
        Console.Clear();
        AnsiConsole.Markup($"[bold yellow]Movimiento realizado:[/]\n");
        ImprimirTablero(tablero);
        if (movimientosBloqueados[jugadorActual])
        {
            break; // Salir del bucle de movimientos restantes para pasar al siguiente jugador
        }
       } 

            // Reducir el enfriamiento de la habilidad al final del turno
            fichaAMover.ReducirEnfriamiento();

            // Cambiar el turno
            turnoActual++;

            // Imprimir el tablero después de cada turno
            Console.Clear();
            ImprimirTablero(tablero);
        }
         // Detener la música de fondo del juego cuando el juego termina
        DetenerMusicaDeFondo();
    }
    
    // Métodos auxiliares (GenerarTablero, QuitarObstaculosAleatorios, CrearFichasDisponibles, MostrarFichasDisponibles, ColocarFichasIniciales, MoverFicha, ImprimirTablero) permanecen sin cambios

    static void IniciarMusicaDeFondo(string musicFilePath)
    {
        DetenerMusicaDeFondo();
        waveOutDevice = new WaveOutEvent();
        audioFileReader = new AudioFileReader(musicFilePath);
        waveOutDevice.Init(audioFileReader);
        waveOutDevice.Play();
    }

    static void DetenerMusicaDeFondo()
    {
        if (waveOutDevice != null)
        {
            waveOutDevice.Stop();
            waveOutDevice.Dispose();
            waveOutDevice = null;
        }

        if (audioFileReader != null)
        {
            audioFileReader.Dispose();
            audioFileReader = null;
        }
    }
     static void MostrarPantallaDeIntroduccion()
    {
        // Limpiar la consola
        Console.Clear();

        // Título del juego
        AnsiConsole.Write(
            new FigletText("LA PANADERIA")
                .Centered()
                .Color(Color.Orange1));

        // Descripción del juego
        AnsiConsole.Markup("[bold yellow]Bienvenido a PAN Runners![/]\n");
        AnsiConsole.Markup("[bold]Imagina que eres un pan, si, un pan y estas cansado de ver a tus camaradas siendo vendidos y comidos delante de tus ojos. Si eres un pan con conciencia tambien puedes tener ojos. Llevas un tiempo planeando tu escape y ahora por fin es el momento de llevarlo a cabo. El objetivo es huir de tu propio creador, alguien en quien pensaste que podias confiar, pero no! Debes correr lejos de el cuanto antes o te venderan para ser comido! O aun peor! Seras botado a la basura para nunca mas ser visto. Asi que huye! Corre! Y sobre todo no mires atras, tu tamaño comparado al del panadero puede ser un poco intimidante. .[/]\n");
        AnsiConsole.Markup("[italic]¡Se un pan, usa tus habilidades de pan y compite contra otros panes para alcanzar la salida![/]\n\n");

        // Instrucciones básicas
        AnsiConsole.Markup("[bold underline]Instrucciones Básicas:[/]\n");
        AnsiConsole.Markup("* Usa las flechas direccionales para mover tu ficha.\n");
        AnsiConsole.Markup("* Pulsa 'Z' para usar tu habilidad especial.\n");
        AnsiConsole.Markup("* Evita los obstáculos '#' y las trampas 'T' y 'P'.\n");
        AnsiConsole.Markup("* Llega al centro del tablero 'F' para ganar.\n");
        AnsiConsole.Markup("* Las trampas P te teletrasportaran a un lugar completamente aleatorio del mapa, las T tienen dos posibles efectos:Te agarro el panadero, este hara que solo puedas moverte una vez mas en tu turno, Exceso de sal ,esta hara que el enfriamiento de tu habilidad aumente en dos turnos(el enfriamiento por defecto son tres turnos).\n");
        AnsiConsole.Markup("* Cuando caes en una trampa ese movimiento no se te descontara pero ten cuidado con sus efectos, esto es riesgo recompensa.\n");
        AnsiConsole.Markup("* ¡Buena suerte!\n\n");

        // Créditos
        AnsiConsole.Markup("[bold underline]Desarrollado por:[/]\n");
        AnsiConsole.Markup("Ivan Rodolfo Romero Rivera \n");
    }

    static char[,] GenerarTablero(int n)
    {
        char[,] tablero = new char[n, n];

        // Inicializar el tablero con espacios vacíos
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                tablero[i, j] = '#'; // Inicialmente lleno de obstáculos
            }
        }

        // Generar un espiral de obstáculos y casillas normales
        int layer = 0;
        while (layer < n / 2)
        {
            for (int i = layer; i < n - layer; i++)
            {
                tablero[layer, i] = '.'; // Fila superior de la capa
                tablero[n - layer - 1, i] = '.'; // Fila inferior de la capa
            }
            for (int j = layer; j < n - layer; j++)
            {
                tablero[j, layer] = '.'; // Columna izquierda de la capa
                tablero[j, n - layer - 1] = '.'; // Columna derecha de la capa
            }
            layer += 2;
        }

                // Añadir trampas en lugares aleatorios dentro del espiral
        Random rand = new Random();
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (tablero[i, j] == '.' && rand.Next(0, 10) < 2) // Añadir trampas con cierta probabilidad
                {
                    if (rand.Next(0, 2) == 0)
                    {
                        tablero[i, j] = 'P'; // Añadir la trampa "cesta de pan"
                    }
                    else
                    {
                        tablero[i, j] = 'T'; // Añadir otras trampas
                    }
                }
            }
        }
        // Asegurar que la meta no se quede obstruida por obstaculos
       Random KK = new Random();
    int hueco = KK.Next(1, 5);
    switch (hueco)
    {
        case 1:
            tablero[centro - 1, centro] = '.'; // Casilla arriba del centro
            break;
        case 2:
            tablero[centro + 1, centro] = '.'; // Casilla abajo del centro
            break;
        case 3:
            tablero[centro, centro - 1] = '.'; // Casilla a la izquierda del centro
            break;
        case 4:
            tablero[centro, centro + 1] = '.'; // Casilla a la derecha del centro
            break;
        default:
            AnsiConsole.MarkupLine("[red] NO PASA NADA.[/]");
            break;
    }
        // Quitar obstáculos de forma aleatoria para conectar las casillas normales
        QuitarObstaculosAleatorios(tablero);

        return tablero;
    }

    static void QuitarObstaculosAleatorios(char[,] tablero)
    {
        int n = tablero.GetLength(0);
        Random rand = new Random();

        // Quitar un número determinado de obstáculos para conectar las casillas normales
        int obstaculosAQuitar = n * n / 10; // Ajusta este número según sea necesario
        int quitados = 0;

        while (quitados < obstaculosAQuitar)
        {
            int i = rand.Next(1, n - 1);
            int j = rand.Next(1, n - 1);

            if (tablero[i, j] == '#')
            {
                tablero[i, j] = '.'; // Quitar el obstáculo
                quitados++;
            }
        }
    }

    static List<Ficha> CrearFichasDisponibles()
    {
        // Crear 5 fichas disponibles con diferentes habilidades, nombres y trasfondos
        return new List<Ficha>
        {
            new Ficha('A', 2, "Descomposición Rápida", "Pan con Moho", "Una vez fue el rey de la panadería, pero ahora está buscando venganza contra aquellos que lo dejaron en el fondo de la cesta. ¡Cuidado con su habilidad de descomposición!"),
            new Ficha('B', 3, "Melodía Mágica", "Pan de Flauta", "Un pan talentoso con una flauta mágica. Sus melodías pueden hipnotizar a cualquiera, haciendo que aparezcan mas trampas T para obstaculizar el paso a otros jugadores."),
            new Ficha('C', 4, "Dulzura Pegajosa", "Pan Dulce", "Un encantador pan que es tan pegajoso como dulce. Su misión es intentar dispersar a los jugadores con trampas P ."),
            new Ficha('D', 2, "Gravedad", "Pan Planetario", "Este pan viene de una panadería galáctica, y su habilidad para manipular la gravedad le permite moverse de formas locas saltando sobre cualquier cosa en su camino."),
            new Ficha('E', 3, "Camuflaje de Migajas", "Pan Color Crimen", "Un pan misterioso con un pasado turbio. Usa su camuflaje de migajas para moverse sin ser detectado por las trampas P.")
        };
    }

    static void MostrarFichasDisponibles(List<Ficha> fichas)
    {
        for (int i = 0; i < fichas.Count; i++)
        {
            AnsiConsole.Markup($"{i + 1}. [bold yellow]{fichas[i].Simbolo}[/] - [bold green]{fichas[i].Nombre}[/] - {fichas[i].Habilidad}\n");
            AnsiConsole.Markup($"[italic]{fichas[i].Trasfondo}[/]\n\n");
            AnsiConsole.Markup($"[bold]Velocidad:[/] {fichas[i].Velocidad}\n\n");
        }
    }

    static void ColocarFichasIniciales(char[,] tablero, List<Ficha> fichas, int filaInicial, int columnaInicial)
    {
        foreach (var ficha in fichas)
        {
            ficha.PosicionFila = filaInicial;
            ficha.PosicionColumna = columnaInicial;
            tablero[ficha.PosicionFila, ficha.PosicionColumna] = ficha.Simbolo;
        }
    }

     static bool MoverFicha(char[,] tablero, Ficha ficha, int nuevaFila, int nuevaColumna, ref bool movimientoBloqueado, ref int turnoActual)
    {
        int filaActual = ficha.PosicionFila;
        int columnaActual = ficha.PosicionColumna;

        // Comprueba que el movimiento sea válido
        if (nuevaFila >= 0 && nuevaFila < tablero.GetLength(0) && nuevaColumna >= 0 && nuevaColumna < tablero.GetLength(1))
        {
            if (tablero[nuevaFila, nuevaColumna] == '#')
            {
                AnsiConsole.Markup("[red]Movimiento inválido. Inténtalo de nuevo.[/]\n");
                return false; // Movimiento inválido debido a un obstáculo
            }
            else if (tablero[nuevaFila, nuevaColumna] == 'T')
            {
                // Guardar la posición anterior de la ficha
                int filaAnterior = ficha.PosicionFila;
                int columnaAnterior = ficha.PosicionColumna;

                // Mover la ficha a la trampa
                ficha.PosicionFila = nuevaFila;
                ficha.PosicionColumna = nuevaColumna;
                tablero[ficha.PosicionFila, ficha.PosicionColumna] = ficha.Simbolo;

                // Activar trampa general
                TrampaGeneral trampaGeneral = new TrampaGeneral();
                bool trampaActivada = false;
                trampaGeneral.Activar(tablero, ref ficha, ref movimientoBloqueado, ref trampaActivada, ref turnoActual);

                // Borrar la ficha de la posición anterior en el tablero si es necesario
                if (trampaActivada)
                {
                    tablero[filaAnterior, columnaAnterior] = '.';
                }
            }
            else if (tablero[nuevaFila, nuevaColumna] == 'P')
            {
                // Guardar la posición anterior de la ficha
                int filaAnterior = ficha.PosicionFila;
                int columnaAnterior = ficha.PosicionColumna;

                // Activar trampa "cesta de pan"
                TrampaCestaPan trampaCestaPan = new TrampaCestaPan();
                bool trampaActivada = false;
                trampaCestaPan.Activar(tablero, ref ficha, ref trampaActivada);

                // Borrar la ficha de la posición anterior en el tablero si es necesario
                if (trampaActivada)
                {
                    tablero[filaAnterior, columnaAnterior] = '.';
                }
            }
            else
            {
                // Mueve la ficha
                tablero[filaActual, columnaActual] = '.';
                tablero[nuevaFila, nuevaColumna] = ficha.Simbolo;
                ficha.PosicionFila = nuevaFila;
                ficha.PosicionColumna = nuevaColumna;

                 // Verificar si el jugador ha llegado a la casilla del medio del mapa
                if (ficha.PosicionFila == centro && ficha.PosicionColumna == centro)
                {
                AnsiConsole.Markup($"[bold yellow]¡{jugadores[jugadorActual]} ha ganado![/]\n");
                Environment.Exit(0); // Termina el juego si un jugador ha ganado
                }

                return true; // Movimiento válido
            }
        }
        else
        {
            AnsiConsole.Markup("[red]Movimiento inválido. Inténtalo de nuevo.[/]\n");
        }
        return false; // Movimiento inválido fuera de los límites del tablero o debido a una trampa 'P'
    }

    static bool VerificarVictoria(Ficha ficha)
    {
        return ficha.PosicionFila == centro && ficha.PosicionColumna == centro;
    }

    static void ImprimirTablero(char[,] tablero)
{
    int n = tablero.GetLength(0);
    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n; j++)
        {
            char simbolo = tablero[i, j];
            switch (simbolo)
            {
                case 'F':
                    AnsiConsole.Markup("[green]F [/]");
                    break;
                case '#':
                    AnsiConsole.Markup("[red]# [/]");
                    break;
                case 'T':
                    AnsiConsole.Markup("[blue]T [/]");
                    break;
                case 'P':
                    AnsiConsole.Markup("[yellow]P [/]");
                    break;
                default:
                    AnsiConsole.Markup($"[white]{simbolo} [/]");
                    break;
            }
        }
        AnsiConsole.WriteLine(); // Asegúrate de que cada fila se imprima en una nueva línea
    }
}
}
class Ficha
{
    public char Simbolo { get; set; }
    public int Velocidad { get; set; }
    public string Habilidad { get; set; }
    public string Nombre { get; set; }
    public string Trasfondo { get; set; }
    public int PosicionFila { get; set; }
    public int PosicionColumna { get; set; }
    public bool HabilidadDisponible { get; set; } = true; // Propiedad para controlar si la habilidad está disponible
    public int EnfriamientoHabilidad { get; set; } = 0; // Propiedad para controlar el enfriamiento de la habilidad

    public Ficha(char simbolo, int velocidad, string habilidad, string nombre, string trasfondo)
    {
        Simbolo = simbolo;
        Velocidad = velocidad;
        Habilidad = habilidad;
        Nombre = nombre;
        Trasfondo = trasfondo;
        PosicionFila = 0;
        PosicionColumna = 0;
    }

    public void UsarHabilidad(char[,] tablero)
    {
        if (HabilidadDisponible && EnfriamientoHabilidad == 0)
        {
            AnsiConsole.Markup($"[bold green]Usando la habilidad: {Habilidad}[/]\n");

            switch (Habilidad)
            {
                case "Descomposición Rápida":
                    DescomponerObstaculos(tablero);
                    break;
                case "Melodía Mágica":
                    GenerarTrampas(tablero);
                    break;
                case "Dulzura Pegajosa":
                    AtraerFichasEnemigas(tablero);
                    break;
                case "Gravedad":
                    MoverConGravedad(tablero);
                    break;
                case "Camuflaje de Migajas":
                    EliminarTrampasAdyacentes(tablero);
                    break;
            }

            HabilidadDisponible = false;
            EnfriamientoHabilidad = 3; // Establece un enfriamiento de 3 turnos
        }
        else if (EnfriamientoHabilidad > 0)
        {
            AnsiConsole.Markup("[red]Habilidad en enfriamiento. Debes esperar más turnos.[/]\n");
        }
        else
        {
            AnsiConsole.Markup("[red]Habilidad no disponible.[/]\n");
        }
    }

    public void ReducirEnfriamiento()
    {
        if (EnfriamientoHabilidad > 0)
        {
            EnfriamientoHabilidad--;
            if (EnfriamientoHabilidad == 0)
            {
                HabilidadDisponible = true; // La habilidad está disponible nuevamente cuando el enfriamiento llega a 0
            }
        }
    }

    public void AumentarEnfriamiento(int turnos)
    {
        EnfriamientoHabilidad += turnos;
    }

    private void DescomponerObstaculos(char[,] tablero)
    {
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nuevaFila = PosicionFila + dx[i];
            int nuevaColumna = PosicionColumna + dy[i];

            if (nuevaFila >= 0 && nuevaFila < tablero.GetLength(0) && nuevaColumna >= 0 && nuevaColumna < tablero.GetLength(1))
            {
                if (tablero[nuevaFila, nuevaColumna] == '#')
                {
                    tablero[nuevaFila, nuevaColumna] = '.';
                    AnsiConsole.Markup($"[bold green]Obstáculo descompuesto en ({nuevaFila}, {nuevaColumna}).[/]\n");
                }
            }
        }
    }

    private void GenerarTrampas(char[,] tablero)
   {
    Random rand = new Random();
    int n = tablero.GetLength(0);
    int trampasGeneradas = 0;

    while (trampasGeneradas < 3) // Generar 3 trampas T
    {
        int fila = rand.Next(n);
        int columna = rand.Next(n);

        if (tablero[fila, columna] == '.')
        {
            tablero[fila, columna] = 'T';
            trampasGeneradas++;
            AnsiConsole.Markup($"[bold green]Trampa T generada en ({fila}, {columna}).[/]\n"); // Imprimir la nueva trampa
        }
    }
    }
    private void AtraerFichasEnemigas(char[,] tablero)
    {
        Random rand = new Random();
    int n = tablero.GetLength(0);
    int trampasGeneradas = 0;

    while (trampasGeneradas < 3) // Generar 3 trampas T
    {
        int fila = rand.Next(n);
        int columna = rand.Next(n);

        if (tablero[fila, columna] == '.')
        {
            tablero[fila, columna] = 'P';
            trampasGeneradas++;
            AnsiConsole.Markup($"[bold green]Trampa P generada en ({fila}, {columna}).[/]\n"); // Imprimir la nueva trampa
        }
    }
    }

    private void MoverConGravedad(char[,] tablero)
    {
        int[] dx = { -2, 2, 0, 0 };
        int[] dy = { 0, 0, -2, 2 };

        Random rand = new Random();
        int nuevaFila, nuevaColumna;
        do
        {
            nuevaFila = PosicionFila + dx[rand.Next(dx.Length)];
            nuevaColumna = PosicionColumna + dy[rand.Next(dy.Length)];
        } while ((nuevaFila < 0 || nuevaFila >= tablero.GetLength(0) || nuevaColumna < 0 || nuevaColumna >= tablero.GetLength(1)) || (nuevaFila == PosicionFila && nuevaColumna == PosicionColumna));

        // Actualizar la posición antigua
        tablero[PosicionFila, PosicionColumna] = '.';

        // Mover la ficha a la nueva posición
        PosicionFila = nuevaFila;
        PosicionColumna = nuevaColumna;
        tablero[PosicionFila, PosicionColumna] = Simbolo;

        AnsiConsole.Markup($"[bold green]Ficha movida a ({PosicionFila}, {PosicionColumna}) saltando un obstáculo.[/]\n");
    }

    private void EliminarTrampasAdyacentes(char[,] tablero)
    {
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nuevaFila = PosicionFila + dx[i];
            int nuevaColumna = PosicionColumna + dy[i];

            if (nuevaFila >= 0 && nuevaFila < tablero.GetLength(0) && nuevaColumna >= 0 && nuevaColumna < tablero.GetLength(1))
            {
                if (tablero[nuevaFila, nuevaColumna] == 'T' || tablero[nuevaFila, nuevaColumna] == 'P')
                {
                    tablero[nuevaFila, nuevaColumna] = '.';
                    AnsiConsole.Markup($"[bold green]Trampa eliminada en ({nuevaFila}, {nuevaColumna}).[/]\n");
                }
            }
        }
    }
}


class TrampaGeneral
{
    private static Random rand = new Random();

    public void Activar(char[,] tablero, ref Ficha ficha, ref bool movimientoBloqueado, ref bool trampaActiva, ref int turnoActual)
    {
        int tipoTrampa = rand.Next(1, 3); // Generar un número aleatorio entre 1 y 2 para elegir el tipo de trampa
        switch (tipoTrampa)
        {
            case 1:
                // Efecto "Te agarró el panadero"
                movimientoBloqueado = true;
                trampaActiva = true;
                AnsiConsole.Markup("[red]¡Te agarró el panadero! Haz tu ultimo movimiento.[/]\n");
                AnsiConsole.Markup("[red]Pulsa cualquier tecla para continuar.[/]\n");
                Console.ReadKey();

                // Finalizar el turno actual y pasar al siguiente
                turnoActual++;
                break;
            case 2:
                // Efecto "Exceso de sal"
                ficha.AumentarEnfriamiento(2); // Aumentar el enfriamiento de la habilidad en 2 turnos
                trampaActiva = true;
                AnsiConsole.Markup("[red]¡Exceso de sal! El enfriamiento de tu habilidad aumenta en 2 turnos.[/]\n");
                AnsiConsole.Markup("[red]Pulsa cualquier tecla para continuar.[/]\n");
                Console.ReadKey();
                break;
        }
    }
}

class TrampaCestaPan
{
    private static Random rand = new Random();

    public void Activar(char[,] tablero, ref Ficha ficha, ref bool trampaActiva)
    {
        // Mover ficha a una posición aleatoria que no sea trampa u obstáculo
        MoverFichaAleatoria(tablero, ref ficha);
        AnsiConsole.Markup("[red]¡Te fuiste en la cesta de pan! La ficha se ha movido a una posición aleatoria.[/]\n");
        AnsiConsole.Markup("[red]Pulsa cualquier tecla para continuar.[/]\n");
        Console.ReadKey();

        // Convertir la trampa en una casilla normal después de teletransportar
        tablero[ficha.PosicionFila, ficha.PosicionColumna] = ficha.Simbolo; // Asegurarse de imprimir la ficha en la nueva posición
        trampaActiva = true;
    }

    private void MoverFichaAleatoria(char[,] tablero, ref Ficha ficha)
    {
        int n = tablero.GetLength(0);
        int nuevaFila, nuevaColumna;
        do
        {
            nuevaFila = rand.Next(0, n);
            nuevaColumna = rand.Next(0, n);
        } while (tablero[nuevaFila, nuevaColumna] == '#' || tablero[nuevaFila, nuevaColumna] == 'T' || 
                 (nuevaFila == ficha.PosicionFila && nuevaColumna == ficha.PosicionColumna));

        // Actualizar la posición antigua
        tablero[ficha.PosicionFila, ficha.PosicionColumna] = '.';

        // Mover la ficha a la nueva posición
        ficha.PosicionFila = nuevaFila;
        ficha.PosicionColumna = nuevaColumna;
        tablero[ficha.PosicionFila, ficha.PosicionColumna] = ficha.Simbolo;
    }
}
