using System.IO;
using System;

namespace Program2
{
    class Program
    {
        //driver to run the program
        static void driver()
        {
            //newFeed object required to pass simulated feed to
            LiveFeed newFeed = new LiveFeed();
            //observers that will subscribe to updates
            LocalStocks localStock = new LocalStocks();
            newFeed.subscribe(localStock.averages);
            newFeed.subscribe(localStock.recordClose);
            newFeed.subscribe(localStock.selectedStocks);

            //counter to count blank lines in ticker.dat
            int count = 0;
            //string to hold StreamReader.ReadLine()
            string line = "";
            //string array to hold split line
            string[] splitLine;
            //Max number of blank lines to read it before notifying the observers the file has ended
            int MAX = 2;
            //Next 3 variables only used to illustrate subscribing/unsubscribing
            //Bool to notify the system an observer is waiting to subscribe
            bool subFlag = false;
            //int to keep track of iterations in while loop
            int counter = 0;
            int TRIGGER = 130;

            StreamReader reader = File.OpenText("Ticker.dat");
            //Keep reading while there is something to read in
            while ((line = reader.ReadLine()) != null)
            {
                //if two blank rows are read in a row, notify the observers the file has ended
                if (line == "")
                {
                    //Next two if statements only used to simulate unsubscribing and subscribing during a live feed
                    if (counter < TRIGGER)
                    {
                        newFeed.unSub(localStock.averages);                        
                    }
                    if(subFlag)
                    {
                        newFeed.subscribe(localStock.averages);
                    }    
                    //keep track of how many blank lines have been read in                  
                    count++;
                    //flag only used to simulate unsub/resubbing
                    subFlag = true;
                    if (count == MAX)
                    {
                        //send the same data again 
                        splitLine = line.Split(' ');
                        newFeed.ParseNewFeed(splitLine);
                    }
                }
                else
                {
                    count = 0;
                    //removes the double white space after the second double in the dat file
                    line = line.Replace("  ", " ");
                    //split up each word or number in the string
                    splitLine = line.Split(' ');
                    //send the string array off to the subject for parsing
                    newFeed.ParseNewFeed(splitLine);
                }
                counter++;
            }
            reader.Close();
        }
           
        static void Main(string[] args)
        {
            driver();
            Console.WriteLine("The simulation has been completed.\nThe new files are: Averages.dat, Record Close.dat, and Selected Stocks.dat.\n",
                              "Press any key to exit the program.");
            Console.ReadKey(true);
        }
    }
}
