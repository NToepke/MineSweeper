using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Forms;

using Button = System.Windows.Controls.Button;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MessageBox = System.Windows.MessageBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace MineSweeper
{
    //contains enum for flagType for use across the program
    public class TypeEnums 
    {
        public enum FlagType //used to determine if a minespot is unknown, flagged or revealed
        {
            Unknown = 0, Flagged = 1, Revealed = 2
        }
        public static FlagType toFlagType(char a) //for encoding, saves, loads, and general ease of access
        {
            if (a == '0')
                return FlagType.Unknown;
            else if (a == '1')
                return FlagType.Flagged;
            return FlagType.Revealed;
        }
    }


    public class MineSpot //class for all spots in the minefield
    {
        public bool isBomb = false; //default is never a bomb
        public TypeEnums.FlagType flag; //unknown, flagged or revealed
        public int neighbors; //How many neighbors are bombs, 9 for being a bomb

        public MineSpot()
        {
            neighbors = 0;
            flag = TypeEnums.FlagType.Unknown;
        }
        public MineSpot(int neighbors)
        {
            this.neighbors = neighbors;
            flag = TypeEnums.FlagType.Unknown;
        }
        public MineSpot(TypeEnums.FlagType flag, int neighbors)
        {
            this.flag = flag;
            this.neighbors = neighbors;
        }

        public MineSpot(char a, char b) //constructor for loading saves
        {
            this.flag = TypeEnums.toFlagType(a);
            this.neighbors = (int)Char.GetNumericValue(b);
        }

        public virtual string toString() //for debugging
        {
            return (flag.ToString() + neighbors);
        }

        //given the parameter, set the MineSpot's flag
        // 0 for unknown
        // 1 for flagged
        // 2 for revealed
        public void setFlag(int a)
        {
            if (a == 0)
                this.flag = TypeEnums.FlagType.Unknown;
            else if (a == 1)
                this.flag = TypeEnums.FlagType.Flagged;
            else
                this.flag = TypeEnums.FlagType.Revealed;
        }



    }


    public class Bomb : MineSpot //class inherits minespot, represents any spots that are bombs
    {
        public Bomb()
        {
            isBomb = true;
            neighbors = 9;
            flag = TypeEnums.FlagType.Unknown;
        }

        public Bomb(TypeEnums.FlagType flag, int neighbors)
        {
            isBomb= true;
            this.flag = flag;
            this.neighbors = neighbors;
        }

        public Bomb(char a, char b) //constructor for saving, loading, and encoding
        {
            isBomb = true;
            this.flag = TypeEnums.toFlagType(a);
            this.neighbors = (int)Char.GetNumericValue(b);
        }

        public override string toString() //for debugging
        {
            return ("Bomb," + flag.ToString());
        }
    }




    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string lastDirectory = @"C:\"; //used for logging the last directory for nicer save and load dialogs

        //player struct logs name, wins and losses for display
        protected struct Player
        {
            public string name; //name of player
            public int wins; //# of recorded wins
            public int losses; //# of recorded losses

            public Player(string name,int a,int b) 
            {
                this.name=name;
                wins = a;
                losses = b;
            }

            public Player(string encoded) //constructor for encoded players from saves
            {
                this.name=encoded.Substring(0,encoded.IndexOf(';'));
                //debug line
                //MessageBox.Show(this.name);
                string tempSubstring = encoded.Substring(encoded.IndexOf(';') + 1);
                //debug line
                //MessageBox.Show(tempSubstring);
                this.wins=Int32.Parse(tempSubstring.Substring(0,tempSubstring.IndexOf(';')));
                tempSubstring=tempSubstring.Substring(tempSubstring.IndexOf(';')+1);
                //debug line
                //MessageBox.Show(tempSubstring);
                this.losses = Int32.Parse(tempSubstring);
            }

            public string encode() //turns a player struct into an encoded string
            {
                return (name + ";" + wins.ToString() + ";" + losses.ToString());
            }


        }

        List<MineSpot> mines = new List<MineSpot>(); //the backend logical buttons that can be clicked, stores data
        List<Button> bombSpots = new List<Button>(); //each button currently representing a bomb
        List<Button> minefield = new List<Button>(); //the physical buttons in the window
        List<Thickness> margins = new List<Thickness>(); //setup for the layout of buttons on the screen
        Player currentPlayer = new Player("Player1", 0, 0); //current player, creates new player on startup
        int totalFlagged = 0; //used for tracking how many flags are placed on the minefield
        bool noSave = true; //tracks if the file was recently saved, used to disable the save button

        public MainWindow()
        {
            InitializeComponent();
            //on startup, load a random 5x5 minefield
            Five_Click(null, null);

            //Obsolete, how the game started up in development
            //tempGenerateMines();
            //generateThickness();
        }

        //open a dialog to load a player profile from file
        protected Player loadPlayer()
        {
            //create a temp player to load into
            Player temp = new Player("Player1",0,0);
            OpenFileDialog fp = new OpenFileDialog();
            fp.Title = "Open Player Profile";
            fp.Filter = "PLY files|*.ply";
            fp.InitialDirectory = lastDirectory;
            try
            {
                if (fp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //file exists
                    //read the file to raw
                    var sr = new StreamReader(fp.FileName);
                    string raw = sr.ReadToEnd();
                    //debug line
                    //MessageBox.Show(raw);
                    //call player constructor and save the new player to return
                    temp = new Player(raw);
                }
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Error Opening File!");
            }
            return temp;
        }

        protected void savePlayer()
        {
            SaveFileDialog fp = new SaveFileDialog();
            fp.Title = "Save Player Profile";
            fp.Filter = "PLY files|*.ply";
            fp.InitialDirectory = lastDirectory;
            try
            {
                if (fp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    lastDirectory = fp.FileName;

                    string tempName = lastDirectory.Substring(lastDirectory.LastIndexOf('\\') + 1);
                    tempName = tempName.Substring(0,tempName.LastIndexOf("."));
                    if(currentPlayer.name == "player1")
                        currentPlayer.name = tempName;

                    string encoded = currentPlayer.encode();
                    StreamWriter sw = new StreamWriter(lastDirectory);
                    sw.Write(encoded);
                    sw.Close();
                }
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Error Saving File!");
            }
            Save.IsEnabled = false;

        }
        //creates a list to generate the set margins for each button in the minefield for spacing
        public void generateThickness()
        {
            //clear old margins
            margins.Clear();
            //debug line
            //MessageBox.Show(mines.Count.ToString());
            //loop thru square of buttons, setting proper spacing for each button
            for (int i = 0; i < Math.Sqrt(mines.Count); i++)
            {
                for(int j = 0; j < Math.Sqrt(mines.Count); j++)
                {
                    //this is the spacing set between mine buttons
                    Thickness temp = new Thickness(10 + (j * 40), 10 + (i*40), 0, 0);
                    //add to list
                    margins.Add(temp);
                    //debug line
                    //MessageBox.Show(j.ToString());
                }
            }
            //continue to generate the actual buttons of the minefield
            generateMinefield();
        }

        //Built for testing, obsolete
        //public bool tempGenerateMines()
        //{
        //    mines.Add(new MineSpot(0));
        //    mines.Add(new MineSpot(1));
        //    mines.Add(new MineSpot(1));
        //    mines.Add(new MineSpot(1));
        //    mines.Add(new MineSpot(0));
        //    mines.Add(new MineSpot(1));
        //    mines.Add(new MineSpot(3));
        //    mines.Add(new Bomb());
        //    mines.Add(new MineSpot(3));
        //    mines.Add(new MineSpot(1));
        //    mines.Add(new Bomb());
        //    mines.Add(new MineSpot(3));
        //    mines.Add(new Bomb());
        //    mines.Add(new MineSpot(3));
        //    mines.Add(new Bomb());
        //    mines.Add(new MineSpot(2));
        //    mines.Add(new MineSpot(3));
        //    mines.Add(new MineSpot(2));
        //    mines.Add(new MineSpot(2));
        //    mines.Add(new MineSpot(1));
        //    mines.Add(new MineSpot(1));
        //    mines.Add(new Bomb());
        //    mines.Add(new MineSpot(1));
        //    mines.Add(new MineSpot(0));
        //    mines.Add(new MineSpot(0));

        //    //temp field, 5x5, 5 mines
        //    // 0 1 1 1 0
        //    // 1 3 X 3 1
        //    // X 3 X 3 X
        //    // 2 3 2 2 1
        //    // 1 X 1 0 0
        //    return true;
        //}

        //generates the buttons on the front end for play
        public bool generateMinefield()
        {
            //clear old buttons
            foreach(Button b in minefield)
            {
                Field.Children.Remove(b);
            }
            minefield.Clear();

            //generate all new buttons
            for(int i = 0; i < mines.Count; i++)
            {
                Button temp = new Button();
                temp.HorizontalAlignment = HorizontalAlignment.Left;
                temp.VerticalAlignment = VerticalAlignment.Top;

                if (mines[i].flag == TypeEnums.FlagType.Revealed) //set the content of the button to reflect if its revealed or flagged or unknown
                    temp.Content = mines[i].neighbors.ToString();
                else if (mines[i].flag == TypeEnums.FlagType.Flagged)
                    temp.Content = "F";
                else
                    temp.Content = "*";
                temp.Height = 25;
                temp.Width = 25;
                temp.Margin = margins[i]; //use the generated margins to space the buttons uniformly
                temp.Name = "N"+(mines[i].neighbors)+"i"+i; 
                //Name is basis for passing some key information between functions in the .CS file.
                //Character 0 is a buffer
                //Character 1 is the number of neighbors that are bombs, 9 means the button IS a bomb.
                //Character 2 is a buffer
                //Character 3->length is the index of the associated bomb in the mines list.

                if(mines[i].isBomb) //if a bomb, add the bomb click method to the button
                {
                    temp.Click += Bomb_Click;
                    bombSpots.Add(temp);
                }
                else //if(mines[i].isBomb == false)
                {
                    temp.Click += Clear_Click; //the button does not represent a bomb, give it  the clear click method
                }
                temp.MouseRightButtonDown += Flag_MouseClick; //all buttons can be flagged

                //debug line
                //MessageBox.Show("L" + i);

                //add the button to the screen
                Field.Children.Add(temp);
                //add the button to the backend list
                minefield.Add(temp);
            }
            //update the UI to reflect how many bombs are in the minefield
            Remainder.Header = ("Bombs: " + bombSpots.Count);

            //debug line
            //MessageBox.Show("L" + minefield.Count() + mines.Count());
            return true;
        }

        //opens a dialog to load a file into the program for play
        public void loadMineFieldDialog()
        {
            OpenFileDialog fp = new OpenFileDialog();
            fp.Title = "Open MineField Save";
            fp.Filter = "NST files|*.nst";
            fp.InitialDirectory = lastDirectory;
            try
            {
                if (fp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //read the new minefield into the program from file
                    var sr = new StreamReader(fp.FileName);
                    string raw = sr.ReadToEnd();

                    //check for bad formatting
                    if(raw.Length % 4 != 0)
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    //clear the old minefield out of the program
                    mines.Clear();
                    bombSpots.Clear();
                    totalFlagged = 0;
                    Placed.Header = ("Flags Placed: " + String.Format("{0:000}", totalFlagged));
                    Remainder.Header = ("Bombs: " + bombSpots.Count);


                    //load each 4 characters in and create a new MineSpot or Bomb, given which it is.
                    //Stored in file as 4 characters:
                    //0 for bomb, 1 for minespot
                    //0 for unknown, 1 for flagged, 2 for revealed
                    //# of neighbors, 9 for bomb
                    //always a comma
                    for (int i = 0; i < raw.Length; i+=4)
                    {
                        if(raw[i] == '0')
                            mines.Add(new Bomb(raw[i + 1], raw[i + 2]));
                        else
                            mines.Add(new MineSpot(raw[i + 1], raw[i + 2]));
                        if (raw[i + 3] != ',') //check for proper formatting
                            throw new ArgumentOutOfRangeException();
                        if (raw[i + 1] == '1')
                            totalFlagged++; //if its a flag, add it to program counter
                    }
                    noSave = true; //since freshly loaded, save button must prompt for file name
                }
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Error Opening File!");
            }
            //debug line
            //MessageBox.Show(mines.Count.ToString());

            //update the UI to reflect new number of flags placed
            Placed.Header = ("Flags Placed: " + String.Format("{0:000}", totalFlagged));
            //continue loading the minefield into buttons
            generateThickness();
        }

        //loads a minefield from file, given the name of the file as a parameter
        public void loadMineFieldDialog(string name)
        {
            try
            {
                //read the selected file into the program
                var sr = new StreamReader("assets/templates/"+name);
                string raw = sr.ReadToEnd();

                //debug line
                //MessageBox.Show(raw);

                //check for invalid file structure
                if (raw.Length % 4 != 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                //clear the old minefield out of the program
                mines.Clear();
                bombSpots.Clear();
                totalFlagged = 0;
                Placed.Header = ("Flags Placed: " + String.Format("{0:000}", totalFlagged));

                //load each 4 characters in and create a new MineSpot or Bomb, given which it is.
                //Stored in file as 4 characters:
                //0 for bomb, 1 for minespot
                //0 for unknown, 1 for flagged, 2 for revealed
                //# of neighbors, 9 for bomb
                //always a comma
                for (int i = 0; i < raw.Length; i += 4)
                {
                    if (raw[i] == '0')
                        mines.Add(new Bomb(raw[i + 1], raw[i + 2]));
                    else
                        mines.Add(new MineSpot(raw[i + 1], raw[i + 2]));
                    //MessageBox.Show(i.ToString());
                    if (raw[i + 3] != ',') //check for bad formatting
                        throw new ArgumentOutOfRangeException();
                    if (raw[i + 1] == '1')
                        totalFlagged++;  //if its a flag, add it to program counter
                }
                noSave=true; //since freshly loaded, save button must prompt for file name
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Error Opening File!");
            }
            //debug line
            //MessageBox.Show(mines.Count.ToString());

            //recalculate flags placed and update UI
            Placed.Header = ("Flags Placed: " + String.Format("{0:000}", totalFlagged));
            //continue loading the minefield
            generateThickness();
        }


        //encodes the current minefield into a single string for saving
        public String encodeField()
        {
            String encoded = ""; //return string
            foreach(MineSpot m in mines) //encode each spot into a 3 digit number and a comma
            {
                int temp = 0;
                if (!m.isBomb) //0 for bomb, 1 for nobomb
                    temp += 100;

                if (m.flag == TypeEnums.FlagType.Revealed) //2 for revealed
                    temp += 20;
                else if (m.flag == TypeEnums.FlagType.Flagged) //1 for flagged, 0 for unknown
                    temp += 10;

                temp += m.neighbors; //last digit is number of neighbors

                //debug line
                //MessageBox.Show(m.toString() + "   " + temp);

                //formats the number to always be 3 digits for leading zeroes and adds to return string
                encoded += (String.Format("{0:000}",temp) + ",");
            }
            return encoded;
        }

        //opens a dialog to save the current minefield
        public void saveMineFieldDialog()
        {
            SaveFileDialog fp = new SaveFileDialog();
            fp.Title = "Save MineField";
            fp.Filter = "NST files|*.nst";
            fp.InitialDirectory = lastDirectory;
            try
            {
                if (fp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //on successful dialog result, encode the current minefield and write it to specified file
                    lastDirectory = fp.FileName;
                    string encoded = encodeField();
                    StreamWriter sw = new StreamWriter(lastDirectory);
                    sw.Write(encoded);
                    sw.Close();
                    Save.IsEnabled = false; //disable the save button, since the file IS saved in its current state
                    noSave = false;
                }
            }
            catch(Exception)
            {
                System.Windows.MessageBox.Show("Error Saving File!");
            }
        }

        //saves the current minefield to specified path
        public void saveMineFieldDialog(string PATH)
        {
            try
            {
                //encode the current minefield and write it to last directory
                string encoded = encodeField();
                StreamWriter sw = new StreamWriter(PATH);
                sw.Write(encoded);
                sw.Close();
                Save.IsEnabled = false; //file is saved, disable save button
                noSave = false;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Error Saving File!");
            }
        }

        //When Called, checks if the current minefield has all mines flagged and the player has won
        public void checkWin()
        {

            if (totalFlagged != bombSpots.Count)
                return;
            //redundancy check, make sure there is the right number of flags

            foreach(Button m in bombSpots)
            {
                if ((string)m.Content != "F")
                    return;
            }
            //All bombs have been flagged

            //At this point, the player has completed this minesweeper puzzle.

            currentPlayer.wins++;

            var result = MessageBox.Show("Congratulations! Start a new Game?", "Victory!", MessageBoxButton.YesNo);

            //start a new 5x5 game
            if (result == MessageBoxResult.Yes)
            {
                Five_Click(null, null);
            }
            else if (result == MessageBoxResult.No)
            {
                Exit_Click(null, null);
            }

        }

        //deactivated, was a temporary method for development, replaced with Five_Click()
        //private void New_Click(object sender, RoutedEventArgs e)
        //{
        //    loadMineFieldDialog("5.nst");
        //    generateMinefield();
        //}
        //load a minefield
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            loadMineFieldDialog();
        }
        //save minefield
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            //check if file has a current directory saved to it
            if(noSave == true)
                saveMineFieldDialog();
            saveMineFieldDialog(lastDirectory);
        }
        //opens dialog prompt to save minefield
        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            saveMineFieldDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //Function allows flagging/unflagging of MineSpot buttons
        private void Flag_MouseClick(object sender, MouseEventArgs e)
        {
            //load the required data into the function
            String name = ((Button)sender).Name;
            int index = Int32.Parse(name.Substring(name.IndexOf('i') + 1));
            MineSpot target = mines[index];
            //identify the MineSpot as an object

            //cannot flag a revealed minespot
            if (target.flag == TypeEnums.FlagType.Revealed)
                return;

            //if unflagged, flag the minespot
            if ((string)(((Button)sender).Content) == "*")
            {
                ((Button)sender).Content = "F";
                mines[index].setFlag(1);
                totalFlagged++; //increment the total count of flags and update the UI
                Placed.Header = ("Flags Placed: " + String.Format("{0:000}", totalFlagged));
            }
            else if((string)(((Button)sender).Content) == "F") //otherwise if flagged, unflag the minespot
            {
                ((Button)sender).Content = "*";
                mines[index].setFlag(0);
                totalFlagged--; //decrement the total count of flags and update the UI
                Placed.Header = ("Flags Placed: " + String.Format("{0:000}", totalFlagged));
            }
            Save.IsEnabled = true; //something has changed, allow saving

            if (totalFlagged == bombSpots.Count) //if this could be a correct solution, check for a win
                checkWin();
        }

        //specific function for when a player clicks on a bomb, losing the game
        private void Bomb_Click(object sender, RoutedEventArgs e)
        {
            //debug line
            //MessageBox.Show("Bomb");

            //get required information about triggered Bomb
            String name = ((Button)sender).Name;
            int index = Int32.Parse(name.Substring(name.IndexOf('i')+1));
            Bomb target = (Bomb)mines.ElementAt<MineSpot>(index);
            target.setFlag(2);
            //reveal all bombs for player to see
            foreach (Button b in bombSpots)
            {
                b.Content = "X";
                SolidColorBrush colorBrush = new SolidColorBrush();
                colorBrush.Color = Color.FromRgb(255,0,0);
                b.Foreground = colorBrush;
            }
            //player has lost, add to player data
            currentPlayer.losses++;
            var result = MessageBox.Show("Game Over, Start a new Game?","BOOM", MessageBoxButton.YesNo);

            //restart a new five by five game
            if(result == MessageBoxResult.Yes)
            {
                Five_Click(sender, e); 
            }
            else if(result == MessageBoxResult.No)
            {
                Exit_Click(sender, e);
            }
        }

        //player reveals a clear spot, show the # of neighbors for that minespot
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            //debug line
            //MessageBox.Show("Clear");
            //collect relevant information
            String name = ((Button)sender).Name;
            int index = Int32.Parse(name.Substring(name.IndexOf('i') + 1));
            MineSpot target = mines.ElementAt<MineSpot>(index);
            //if this was a flagged spot, update the counter and UI
            if (target.flag == TypeEnums.FlagType.Flagged)
            {
                totalFlagged--;
                Placed.Header = ("Flags Placed: " + String.Format("{0:000}", totalFlagged));

            }
            //set backing array to reflect that the object has been revealed
            mines[index].setFlag(2);
            //update button to show number of neighbors
            ((Button)sender).Content = ((Button)sender).Name[1];
            //remove the ability to flag this minespot button
            ((Button)sender).MouseRightButtonDown -= Flag_MouseClick; //works great for current games, not so much for saved games that have been loaded.

            Save.IsEnabled = true; //changes have been made, allow saving

            if (totalFlagged == bombSpots.Count) //check if by revealing this spot, the totalflagged could result in a solution
                checkWin();
        }

        //Menu option selected to play a 5x5 map, or default size used
        private void Five_Click(object sender, RoutedEventArgs e)
        {
            //temporary starter use, now defunct
            //loadMineFieldDialog("5.nst");
            //List of all known 5x5 levels to pick from
            List<String> fivebyfive = new List<string>()
            {
                "5.nst",
                "4.nst",
                "3.nst",
                "2.nst",
                "1.nst"
            };
            //randomly select one of the 5x5 maps to play on
            System.Random random = new System.Random();

            int selector = random.Next(fivebyfive.Count);
            loadMineFieldDialog(fivebyfive[selector]);
        }
        //Menu option selected to play a 7x7 map
        private void Seven_Click(object sender, RoutedEventArgs e)
        {
            //List of all known 7x7 levels to pick from
            List<String> sevenbyseven = new List<string>()
            {
                "7.nst",
                "8.nst",
            };
            //randomly select one of the 7x7 maps to play on
            System.Random random = new System.Random();

            int selector = random.Next(sevenbyseven.Count);
            loadMineFieldDialog(sevenbyseven[selector]);
        }
        //menu option selected to play a 9x9 map
        private void Nine_Click(object sender, RoutedEventArgs e)
        {
            //List of all known 9x9 levels to pick from
            List<String> ninebynine = new List<string>()
            {
                "9.nst",
                "10.nst",
            };
            //randomly select one of the 9x9 maps to play on
            System.Random random = new System.Random();

            int selector = random.Next(ninebynine.Count);
            loadMineFieldDialog(ninebynine[selector]);
        }

        //displays information about current player
        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Player Name: "+ currentPlayer.name + "\nTotal Wins: " + currentPlayer.wins + "\nTotal Losses: " + currentPlayer.losses);
        }

        //displays information about bombs on the map
        private void Remain_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Total Bombs: " + bombSpots.Count + "\nBombs Remaining: " + (bombSpots.Count - totalFlagged));
        }
        //displays information on all flags currently placed
        private void Flags_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Total Flags Placed: " + totalFlagged);
        }
        //Run dialog to load a new player profile
        private void ProfileLoad_Click(object sender, RoutedEventArgs e)
        {
            currentPlayer = loadPlayer();
        }
        //save current player profile
        private void ProfileSave_Click(object sender, RoutedEventArgs e)
        {
            savePlayer();
        }
        //load a HARD 5x5 minefield
        private void FiveH_Click(object sender, RoutedEventArgs e)
        {
            loadMineFieldDialog("5H.nst");
        }
        //load a HARD 7x7 Minefield
        private void SevenH_Click(object sender, RoutedEventArgs e)
        {
            loadMineFieldDialog("7H.nst");
        }
        //load a HARD 9x9 minefield
        private void NineH_Click(object sender, RoutedEventArgs e)
        {
            loadMineFieldDialog("9H.nst");
        }

        //click event for the about button
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("MINESWEEPER\nDeveloped by Nathan Toepke");
            MessageBox.Show("Left click to reveal a tile, Right click to flag a tile.\nFlag all bombs to win!");
            MessageBox.Show("When a tile is revealed, it will either blow up,\nor show a number.\nThe number is how many of its neighbors are bombs.");
        }
    }
}
