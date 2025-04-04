using System;
using System.Runtime.InteropServices;
using SDL2;
using System.Text;

class Program
{
    const int SCREEN_WIDTH = 800;
    const int SCREEN_HEIGHT = 600;
    const int MAX_PLAYERS = 4;
    const int QUESTIONS_PER_PLAYER = 3;
    const int MAX_QUESTIONS = 50;
    const int MAX_OPTIONS = 4;
    const int MAX_NAME_LENGTH = 50;
    const int MAX_QUESTION_LENGTH = 256;
    const int MAX_OPTION_LENGTH = 100;

    class Player
    {
        public string name;
        public int score;
        public int currentQuestionIndex;
        public int[] questionIndices;

        public Player()
        {
            name = "";
            score = 0;
            currentQuestionIndex = 0;
            questionIndices = new int[QUESTIONS_PER_PLAYER];
        }
    }

    class Question
    {
        public string question;
        public string[] options;
        public int correctAnswer;
        public bool used;

        public Question()
        {
            question = "";
            options = new string[MAX_OPTIONS];
            correctAnswer = 0;
            used = false;
        }
    }

    static Question[] questions = new Question[MAX_QUESTIONS];
    static Player[] players = new Player[MAX_PLAYERS];
    static int numPlayers = 0;
    static int currentPlayer = 0;

    static IntPtr window = IntPtr.Zero;
    static IntPtr renderer = IntPtr.Zero;
    static IntPtr font = IntPtr.Zero;
    static SDL.SDL_Color textColor = new SDL.SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
    static SDL.SDL_Color highlightColor = new SDL.SDL_Color() { r = 255, g = 215, b = 0, a = 255 };

    static void InitializeSDL()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            Console.WriteLine($"Erreur SDL_Init: {SDL.SDL_GetError()}");
            Environment.Exit(1);
        }

        if (SDL_ttf.TTF_Init() == -1)
        {
            Console.WriteLine($"Erreur TTF_Init: {SDL_ttf.TTF_GetError()}");
            Environment.Exit(1);
        }

        window = SDL.SDL_CreateWindow("Quiz Informatique",
                                    SDL.SDL_WINDOWPOS_CENTERED,
                                    SDL.SDL_WINDOWPOS_CENTERED,
                                    SCREEN_WIDTH, SCREEN_HEIGHT,
                                    SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
        if (window == IntPtr.Zero)
        {
            Console.WriteLine($"Erreur création fenêtre: {SDL.SDL_GetError()}");
            Environment.Exit(1);
        }

        renderer = SDL.SDL_CreateRenderer(window, -1,
                                        SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | 
                                        SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (renderer == IntPtr.Zero)
        {
            Console.WriteLine($"Erreur création renderer: {SDL.SDL_GetError()}");
            Environment.Exit(1);
        }

        string[] fontPaths = {
            "/usr/share/fonts/truetype/freefont/FreeSans.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/ubuntu/Ubuntu-R.ttf"
        };

        foreach (string path in fontPaths)
        {
            font = SDL_ttf.TTF_OpenFont(path, 24);
            if (font != IntPtr.Zero)
                break;
            Console.WriteLine($"Essai police {path} échoué: {SDL_ttf.TTF_GetError()}");
        }

        if (font == IntPtr.Zero)
        {
            Console.WriteLine("\nERREUR: Aucune police trouvée!");
            Console.WriteLine("Installez des polices avec:");
            Console.WriteLine("sudo apt install fonts-freefont-ttf ttf-dejavu-core fonts-liberation");
            Environment.Exit(1);
        }
    }

    static void CloseSDL()
    {
        if (font != IntPtr.Zero) SDL_ttf.TTF_CloseFont(font);
        if (renderer != IntPtr.Zero) SDL.SDL_DestroyRenderer(renderer);
        if (window != IntPtr.Zero) SDL.SDL_DestroyWindow(window);
        SDL_ttf.TTF_Quit();
        SDL.SDL_Quit();
    }

    static void RenderText(string text, int x, int y, SDL.SDL_Color color)
    {
        IntPtr surface = SDL_ttf.TTF_RenderText_Blended(font, text, color);
        if (surface == IntPtr.Zero)
        {
            Console.WriteLine($"Erreur création surface texte: {SDL_ttf.TTF_GetError()}");
            return;
        }

        IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, surface);
        if (texture == IntPtr.Zero)
        {
            Console.WriteLine($"Erreur création texture: {SDL.SDL_GetError()}");
            SDL.SDL_FreeSurface(surface);
            return;
        }

        SDL.SDL_Rect rect = new SDL.SDL_Rect() { x = x, y = y, w = 0, h = 0 };
        SDL.SDL_QueryTexture(texture, out _, out _, out rect.w, out rect.h);
        SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, ref rect);
        
        SDL.SDL_DestroyTexture(texture);
        SDL.SDL_FreeSurface(surface);
    }

    static void InitializeQuestions()
    {
        Random rand = new Random();
        
        // Question 1
        questions[0] = new Question();
        questions[0].question = "Quel langage a inspiré C++?";
        questions[0].options[0] = "C";
        questions[0].options[1] = "Java";
        questions[0].options[2] = "Python";
        questions[0].options[3] = "Assembly";
        questions[0].correctAnswer = 0;
        questions[0].used = false;

        // Question 2
        questions[1] = new Question();
        questions[1].question = "Quelle commande Linux liste les fichiers?";
        questions[1].options[0] = "dir";
        questions[1].options[1] = "ls";
        questions[1].options[2] = "list";
        questions[1].options[3] = "show";
        questions[1].correctAnswer = 1;
        questions[1].used = false;

        // Question 3
        questions[2] = new Question();
        questions[2].question = "Quel est le gestionnaire de paquets de Ubuntu?";
        questions[2].options[0] = "yum";
        questions[2].options[1] = "pacman";
        questions[2].options[2] = "apt";
        questions[2].options[3] = "dnf";
        questions[2].correctAnswer = 2;
        questions[2].used = false;

        for (int i = 3; i < MAX_QUESTIONS; i++)
        {
            questions[i] = new Question();
            questions[i].used = true;
        }
    }

    static void AssignQuestionsToPlayers()
    {
        for (int i = 0; i < 3; i++)
        {
            questions[i].used = false;
        }

        for (int p = 0; p < numPlayers; p++)
        {
            for (int i = 0; i < 3; i++)
            {
                players[p].questionIndices[i] = i;
            }
            
            players[p].currentQuestionIndex = 0;
            players[p].score = 0;
        }
    }

    static void GetNumberOfPlayers()
    {
        SDL.SDL_Event e;
        bool done = false;

        while (!done)
        {
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    Environment.Exit(0);
                }
                else if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                {
                    if (e.key.keysym.sym >= SDL.SDL_Keycode.SDLK_1 && e.key.keysym.sym <= SDL.SDL_Keycode.SDLK_4)
                    {
                        numPlayers = e.key.keysym.sym - SDL.SDL_Keycode.SDLK_0;
                        done = true;
                    }
                    else if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
                    {
                        Environment.Exit(0);
                    }
                }
            }

            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL.SDL_RenderClear(renderer);

            RenderText("QUIZ INFORMATIQUE", SCREEN_WIDTH/2 - 150, 50, highlightColor);
            RenderText("Combien de joueurs? (1-4)", SCREEN_WIDTH/2 - 150, 150, textColor);
            RenderText("Appuyez sur 1, 2, 3 ou 4", SCREEN_WIDTH/2 - 150, 200, textColor);

            SDL.SDL_RenderPresent(renderer);
        }
    }

    static void GetPlayerNames()
    {
        SDL.SDL_Event e;
        StringBuilder name = new StringBuilder(MAX_NAME_LENGTH);
        int currentPlayerInput = 0;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            players[i] = new Player();
        }

        while (currentPlayerInput < numPlayers)
        {
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    Environment.Exit(0);
                }
                else if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                {
                    if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_RETURN && name.Length > 0)
                    {
                        players[currentPlayerInput].name = name.ToString();
                        currentPlayerInput++;
                        name.Clear();
                    }
                    else if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_BACKSPACE && name.Length > 0)
                    {
                        name.Length--;
                    }
                    else if (name.Length < MAX_NAME_LENGTH - 1 && char.IsLetterOrDigit((char)e.key.keysym.sym))
                    {
                        name.Append((char)e.key.keysym.sym);
                    }
                }
            }

            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL.SDL_RenderClear(renderer);

            string prompt = $"Entrez le nom du joueur {currentPlayerInput + 1}:";
            RenderText(prompt, SCREEN_WIDTH/2 - 150, 150, textColor);
            
            string namePrompt = "Nom: " + name.ToString();
            RenderText(namePrompt, SCREEN_WIDTH/2 - 150, 200, textColor);
            RenderText("Appuyez sur Entrée pour valider", SCREEN_WIDTH/2 - 150, 250, textColor);

            SDL.SDL_RenderPresent(renderer);
        }
    }

    static void ShowQuestion()
    {
        Player current = players[currentPlayer];
        if (current.currentQuestionIndex >= 3)
        {
            currentPlayer = (currentPlayer + 1) % numPlayers;
            return;
        }

        int qIndex = current.questionIndices[current.currentQuestionIndex];
        Question q = questions[qIndex];
        
        SDL.SDL_Event e;
        bool answered = false;
        int selectedOption = -1;

        while (!answered)
        {
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    Environment.Exit(0);
                }
                else if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                {
                    if (e.key.keysym.sym >= SDL.SDL_Keycode.SDLK_1 && e.key.keysym.sym <= SDL.SDL_Keycode.SDLK_4)
                    {
                        selectedOption = e.key.keysym.sym - SDL.SDL_Keycode.SDLK_1;
                        answered = true;
                    }
                }
            }

            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL.SDL_RenderClear(renderer);

            string playerInfo = $"Joueur: {current.name} - Score: {current.score}";
            RenderText(playerInfo, 50, 30, textColor);

            RenderText(q.question, 50, 100, highlightColor);

            for (int i = 0; i < MAX_OPTIONS; i++)
            {
                string optionText = $"{i+1}. {q.options[i]}";
                RenderText(optionText, 100, 180 + i * 40, textColor);
            }

            RenderText("Choisissez une option (1-4):", 50, 350, textColor);

            SDL.SDL_RenderPresent(renderer);
        }

        if (selectedOption == q.correctAnswer)
        {
            current.score += 10;
            
            SDL.SDL_SetRenderDrawColor(renderer, 0, 50, 0, 255);
            SDL.SDL_RenderClear(renderer);
            RenderText("Bonne réponse!", SCREEN_WIDTH/2 - 100, 200, highlightColor);
            SDL.SDL_RenderPresent(renderer);
            SDL.SDL_Delay(1500);
        }
        else
        {
            SDL.SDL_SetRenderDrawColor(renderer, 50, 0, 0, 255);
            SDL.SDL_RenderClear(renderer);
            RenderText("Mauvaise réponse!", SCREEN_WIDTH/2 - 100, 200, highlightColor);
            
            string correctAnswer = $"La bonne réponse était: {q.options[q.correctAnswer]}";
            RenderText(correctAnswer, SCREEN_WIDTH/2 - 200, 250, textColor);
            
            SDL.SDL_RenderPresent(renderer);
            SDL.SDL_Delay(2000);
        }

        current.currentQuestionIndex++;
        currentPlayer = (currentPlayer + 1) % numPlayers;
    }

    static bool AllPlayersFinished()
    {
        for (int i = 0; i < numPlayers; i++)
        {
            if (players[i].currentQuestionIndex < 3)
            {
                return false;
            }
        }
        return true;
    }

    static void ShowWinners()
    {
        int maxScore = 0;
        for (int i = 0; i < numPlayers; i++)
        {
            if (players[i].score > maxScore)
            {
                maxScore = players[i].score;
            }
        }

        int winnerCount = 0;
        for (int i = 0; i < numPlayers; i++)
        {
            if (players[i].score == maxScore)
            {
                winnerCount++;
            }
        }

        SDL.SDL_Event e;
        bool done = false;

        while (!done)
        {
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT || e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                {
                    done = true;
                }
            }

            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 50, 255);
            SDL.SDL_RenderClear(renderer);

            if (winnerCount == 1)
            {
                RenderText("VAINQUEUR:", SCREEN_WIDTH/2 - 100, 50, highlightColor);
            }
            else
            {
                RenderText("VAINQUEURS (ex-aequo):", SCREEN_WIDTH/2 - 150, 50, highlightColor);
            }

            int yPos = 150;
            for (int i = 0; i < numPlayers; i++)
            {
                string scoreText = $"{players[i].name}: {players[i].score} points";
                
                if (players[i].score == maxScore)
                {
                    RenderText(scoreText, SCREEN_WIDTH/2 - 150, yPos, highlightColor);
                }
                else
                {
                    RenderText(scoreText, SCREEN_WIDTH/2 - 150, yPos, textColor);
                }
                yPos += 40;
            }

            RenderText("Appuyez sur une touche pour quitter", 
                     SCREEN_WIDTH/2 - 200, 400, textColor);

            SDL.SDL_RenderPresent(renderer);
        }
    }

    static void Main(string[] args)
    {
        InitializeSDL();
        InitializeQuestions();
        
        GetNumberOfPlayers();
        GetPlayerNames();
        AssignQuestionsToPlayers();

        while (!AllPlayersFinished())
        {
            ShowQuestion();
        }

        ShowWinners();
        CloseSDL();
    }
}