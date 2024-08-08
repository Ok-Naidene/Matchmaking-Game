using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using static Windows.UI.Xaml.Application; 

namespace MatchmakingGame
{
    public sealed partial class MainPage : Page
    {
        private List<Card> cards;
        private Card firstSelectedCard;
        private Card secondSelectedCard;
        private Button firstSelectedButton;
        private Button secondSelectedButton;
        private int level;
        private DispatcherTimer timer;
        private int timeLeft;
        private bool isDialogOpen = false;
        private bool timerStarted = false;

        public MainPage()
        {
            this.InitializeComponent();
            StartGame(1);
        }

        private void StartGame(int level)
        {
            this.level = level;
            LevelText.Text = $"Level {level}";

            int gridSizeRows;
            int gridSizeCols;
            int numberOfPairs;
            int timeLimit;

            GetLevelSettings(level, out gridSizeRows, out gridSizeCols, out numberOfPairs, out timeLimit);
            timeLeft = timeLimit;

            ConfigureGrid(gridSizeRows, gridSizeCols);
            InitializeCards(numberOfPairs);
            ShuffleCards();
            DisplayCards(gridSizeRows, gridSizeCols);
        }

        private void GetLevelSettings(int level, out int gridSizeRows, out int gridSizeCols, out int numberOfPairs, out int timeLimit)
        {
            if (level == 1)
            {
                gridSizeRows = 2;
                gridSizeCols = 2;
                numberOfPairs = 2;
                timeLimit = 20;
            }
            else if (level == 2)
            {
                gridSizeRows = 2;
                gridSizeCols = 4;
                numberOfPairs = 4;
                timeLimit = 60;
            }
            else
            {
                gridSizeRows = 4;
                gridSizeCols = 4;
                numberOfPairs = 8;
                timeLimit = 120;
            }
        }

        private void ConfigureGrid(int rows, int cols)
        {
            GameGrid.RowDefinitions.Clear();
            GameGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < rows; i++)
                GameGrid.RowDefinitions.Add(new RowDefinition());

            for (int i = 0; i < cols; i++)
                GameGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        private void InitializeCards(int numberOfPairs)
        {
            cards = new List<Card>();
            for (int i = 0; i < numberOfPairs; i++)
            {
                cards.Add(new Card { ImagePath = $"/Assets/{i}.png" });
                cards.Add(new Card { ImagePath = $"/Assets/{i}.png" });
            }
        }

        private void ShuffleCards()
        {
            var random = new Random();
            for (int i = 0; i < cards.Count; i++)
            {
                int j = random.Next(i, cards.Count);
                var temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
        }

        private void DisplayCards(int rows, int cols)
        {
            GameGrid.Children.Clear();
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var card = cards[row * cols + col];
                    var button = CreateCardButton(card);
                    GameGrid.Children.Add(button);
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                }
            }
        }

        private Button CreateCardButton(Card card)
        {
            var button = new Button { Tag = card };
            button.Content = new Image { Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/card_back.png")) };
            button.Click += CardButton_Click;
            return button;
        }

        private async void CardButton_Click(object sender, RoutedEventArgs e)
        {
            if (!timerStarted)
            {
                StartTimer();
                timerStarted = true;
            }

            var button = sender as Button;
            var card = button?.Tag as Card;

            if (card == null || card.IsMatched || button == firstSelectedButton)
                return;

            button.Content = new Image { Source = new BitmapImage(new Uri(this.BaseUri, card.ImagePath)) };

            if (firstSelectedCard == null)
            {
                firstSelectedCard = card;
                firstSelectedButton = button;
            }
            else
            {
                secondSelectedCard = card;
                secondSelectedButton = button;

                if (firstSelectedCard.ImagePath == secondSelectedCard.ImagePath)
                {
                    firstSelectedCard.IsMatched = true;
                    secondSelectedCard.IsMatched = true;
                    ResetSelections();

                    if (AllCardsMatched())
                        ProceedToNextLevelOrEnd();
                }
                else
                {
                    await Task.Delay(1000);
                    ResetCard(button);
                    ResetCard(firstSelectedButton);
                    ResetSelections();
                }
            }
        }

        private void ResetCard(Button button)
        {
            button.Content = new Image { Source = new BitmapImage(new Uri(this.BaseUri, "/Assets/card_back.png")) };
        }

        private void ResetSelections()
        {
            firstSelectedCard = null;
            secondSelectedCard = null;
            firstSelectedButton = null;
            secondSelectedButton = null;
        }

        private bool AllCardsMatched() => cards.TrueForAll(card => card.IsMatched);

        private void ProceedToNextLevelOrEnd()
        {
            if (level < 3)
                StartGame(level + 1);
            else
                ShowEndingMessage();
        }

        private void StartTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            timeLeft--;
            TimerText.Text = $"Time Left: {timeLeft}s";

            if (timeLeft <= 0)
            {
                timer.Stop();
                ShowLossMessage();
            }
        }

        private async void ShowLossMessage()
        {
            await ShowMessageDialog("Time's Up!", "You ran out of time. Try again!");
            StartGame(1);
        }

        private async void ShowEndingMessage()
        {
            await ShowMessageDialog("Congratulations!", "You've finished all the levels!");
            Current.Exit(); // Exit the application after showing the ending message
        }

        private async Task ShowMessageDialog(string title, string content)
        {
            if (isDialogOpen) return;

            isDialogOpen = true;
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK"
            };

            await dialog.ShowAsync();
            isDialogOpen = false;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        private void ResetGame()
        {
            timer?.Stop();
            timerStarted = false;
            StartGame(1);
        }
    }
}
