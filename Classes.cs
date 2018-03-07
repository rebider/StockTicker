using System.Collections.Generic;
using System.IO;

//Skeleton interface for a subject
public interface Subject
{
    void subscribe(Observer o);
    void unSub(Observer o);
    void notify();
}
//Implementation of the subject interface
public class LiveFeed : Subject
{
    //Public properties to be set by the incoming feed
    public string Date { get; set; }
    public string Company { get; set;}
    public string Ticker { get; set; }
    public double Current { get; set; }
    public double DollarChange { get; set; }
    public double PercentChange { get; set; }
    public double YTDChange { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double PERatio { get; set;}
    //Private fields
    List<Observer> currentObservers;
    //Default constructor
    public LiveFeed()
    {
        currentObservers = new List<Observer>();
    }
    //Parses an incoming newsfeed
    //Input is an array of strings that has been split from a larger string
    //Calls notfiy() each time a line containing new data has been read
    public void ParseNewFeed(string[] feed)
    {        
        //if the incoming string array contanins "" the end of file has been reached
        //re-send the last data to notify the observers the file has been fully read
        if (feed.Length == 1)
        {
            notify();
        }
        else
        {
            //normalize length to 0 based indexer and subtract one to account for trailing whitespace in dat file
            int length = feed.Length - 2;
            //double to be used with TryParse() calls
            double parsedDouble = 0;
            //Checks to see if the incoming feed is a timestamp
            if (!double.TryParse(feed[length], out parsedDouble) && feed[length] != "-")
            {
                Date = string.Join(" ", feed);
            }
            //Incoming array contains new stock data
            else
            {
                //Data is read from the end of the array and assigned to each property
                //Length is pre-decremented backwards through the array
                List<string> toConcat = new List<string>();
                PERatio = double.TryParse(feed[length], out parsedDouble) == true ? parsedDouble : -1;//if the parse succeeds, assign it the value. If not, P/E is negative
                Low = double.Parse(feed[--length]);
                High = double.Parse(feed[--length]);
                YTDChange = double.Parse(feed[--length]);
                PercentChange = double.Parse(feed[--length]);
                DollarChange = double.Parse(feed[--length]);
                Current = double.Parse(feed[--length]);
                Ticker = feed[--length];
                //Remaining strings in the array need to be concatenated to form the company's full name
                for (int i = 0; i < length; i++)
                {
                    toConcat.Add(feed[i]);
                }
                Company = string.Join(" ", toConcat);
                //A full line has been read, need to push the new data to the subscribers
                notify();
            }

        }
    }
    
    //Allows an observer to subscribe to updates
    public void subscribe(Observer o)
    {
        if (!currentObservers.Exists(element => element == o))//check to make sure the observer is not already in the list
        {
            currentObservers.Add(o);
        }        
    }

    //Allows observers to unsubscribe from updates
    public void unSub(Observer o)
    {
        if(currentObservers.Exists(element => element == o))//check to make sure the subscriber is in the list
        {
            currentObservers.Remove(o);
        }
        
    }

    //Iterates through the current observers and updates them with the new data
    public void notify()
    {
        foreach(Observer o in currentObservers)
        {
            o.update(this);
        }
    }
}

//Skeleton interface for all observers
public interface Observer
{
    void update( LiveFeed s);
    void generateReport(params object[] toGenerate);
}

//This observer will gather incoming current prices and generate the average of them for each snapshot
public class AverageObserver : Observer
{
    //Keeps track of the current snapshot being read in
    private string currentSnap;
    //Contains the final average prices for each snapshot
    private List<string> averageList;
    //Temporary containter to hold incoming prices
    private List<double> pricesList;
    //Track last data received
    private string previousTicker;

    public AverageObserver()
    {
        currentSnap = "";
        previousTicker = "";
        averageList = new List<string>();
        pricesList = new List<double>();
    }

    public void update(LiveFeed lf)
    {
        //If the incoming data is the same as previously received, the end of file has been read. 
        //Need to do the last report
        if (lf.Ticker == previousTicker)
        {
            //Iterate through list, find average and add to dictionary
            double temp = 0;
            foreach (double price in pricesList)
            {
                temp += price;
            }
            averageList.Add(currentSnap + " " + (temp / pricesList.Count).ToString());
            averageList.Add("\n");
            object[] toSend = new object[] { averageList };
            generateReport(toSend);
        }

        else
        {
            if (currentSnap == lf.Date)//Check to see if we are updating within the same snapshot
            {
                pricesList.Add(lf.Current);
                previousTicker = lf.Ticker;
            }
            else//Updating first of the sequence or switching snapshots
            {
                if (pricesList.Count > 0)//Switching snapshots. Need to find the average of the previous snapshot, add it to the averageList and reset to a new snapshot
                {
                    //Iterate through list, find average and add to dictionary
                    double temp = 0;
                    foreach (double price in pricesList)
                    {
                        temp += price;
                    }
                    averageList.Add(currentSnap + " " + (temp / pricesList.Count).ToString());
                    averageList.Add("\n");
                    object[] toSend = new object[] { averageList };
                    generateReport(toSend);
                    //Reset prices list and add the current price
                    pricesList.Clear();
                    averageList.Clear();
                }
                pricesList.Add(lf.Current);
                currentSnap = lf.Date;
                previousTicker = lf.Ticker;
            }
        }
    }

    public void generateReport(object[] toWriteArray)
    {
        List<string> toWrite = new List<string>();
        toWrite = (List<string>)toWriteArray[0];
        StreamWriter write = new StreamWriter("Averages.dat", true);

        foreach (string s in toWrite)
        {
            write.WriteLine(s);
        }
        write.Close();
    }
}


//Tracks stocks that close within +/- 1% of their 52-week record high or low
public class RecordCloseObserver : Observer
{
    //List containing the timestamp and all stocks that close within 1% of record high/low
    private List<string> recordCloseList;    
    //Tracks the current incoming snapshot
    private string currentSnap;
    private string lastTicker;

    public RecordCloseObserver()
    {
        currentSnap = "";
        lastTicker = "";
        recordCloseList = new List<string>();        
    }

    public void update(LiveFeed lf)
    {
        //If the incoming data is the same as previously received, the end of file has been read. 
        //Need to do the last report
        if (lastTicker == lf.Ticker)
        {
            recordCloseList.Add("\n");
            object[] toSend = new object[] { recordCloseList };
            generateReport(toSend);
        }
        else
        {
            //1% figure for the current stock
            double currentOnePercent = lf.Current / 100;

            if (lf.Current + currentOnePercent >= lf.High || lf.Current - currentOnePercent <= lf.Low)//check to see if current price is within +/- 1% of 52-week high or low
            {
                if (currentSnap == lf.Date)//check to see if the current feed is from the same snapshot and record ticker, current price, high, and low of the incoming data
                {
                    recordCloseList.Add(lf.Ticker + " " + lf.Current + " " + lf.High + " " + lf.Low);
                }

                else//current feed is from a different snapshot or is the first in the sequence
                {
                    if (recordCloseList.Count > 0)
                    {
                        recordCloseList.Add("\n");
                        object[] toSend = new object[] { recordCloseList };
                        generateReport(toSend);
                        recordCloseList.Clear();
                    }
                    recordCloseList.Add(lf.Date);
                    recordCloseList.Add(lf.Ticker + " " + lf.Current + " " + lf.High + " " + lf.Low);
                }
                currentSnap = lf.Date;
            }            
            lastTicker = lf.Ticker;
        }
    }
    public void generateReport(object[] toWriteArray)
    {
        List<string> toWrite = new List<string>();
        toWrite = (List<string>)toWriteArray[0];
        StreamWriter write = new StreamWriter("Record Close.dat", true);

        foreach (string s in toWrite)
        {
            write.WriteLine(s);
        }
        write.Close();
    }
}

//Grabs all information on the selected stocks
public class SelectedStocksObserver : Observer
{
    //List to hold the subject objects
    List<string> selectedStocks;
    string currentSnap;
    string lastTicker;

    public SelectedStocksObserver()
    {
        selectedStocks = new List<string>();
        currentSnap = "";
        lastTicker = "";
    }

    public void update(LiveFeed lf)
    {
        //If the incoming data is the same as previously received, the end of file has been read. 
        //Need to do the last report
        if (lastTicker == lf.Ticker)
        {
            selectedStocks.Add("\n");
            object[] toSend = new object[] { selectedStocks };
            generateReport(toSend);
        }
        else
        {
            switch (lf.Ticker)
            {
                case "ALL":
                case "BA":
                case "BC":
                case "GBEL":
                case "KFT":
                case "MCD":
                case "TR":
                case "WAG":
                    //if one of the selected stocks comes through, capture all the data and add it to the list
                    if (currentSnap == lf.Date)//check to see that we are in the same snapshot
                    {
                        selectedStocks.Add(lf.Company + " " + lf.Ticker + " " + lf.Current + " " + lf.DollarChange + " " + lf.PercentChange
                                            + " " + lf.YTDChange + " " + lf.High + " " + lf.Low + " " + lf.PERatio);
                    }
                    else//we've switched snapshots, need to generate the report or we are adding the first of the sequence
                    {
                        if (selectedStocks.Count > 0)//generate report
                        {
                            selectedStocks.Add("\n");
                            object[] toSend = new object[] { selectedStocks };
                            generateReport(toSend);
                            selectedStocks.Clear();
                        }
                        selectedStocks.Add(lf.Date);
                        selectedStocks.Add(lf.Company + " " + lf.Ticker + " " + lf.Current + " " + lf.DollarChange + " " + lf.PercentChange
                                            + " " + lf.YTDChange + " " + lf.High + " " + lf.Low + " " + lf.PERatio);
                        currentSnap = lf.Date;
                    }
                    lastTicker = lf.Ticker;
                    break;
                default:
                    lastTicker = lf.Ticker;
                    break;
            }
        }
    }
    public void generateReport(object[] toWriteArray)
    {
        List<string> toWrite = new List<string>();
        toWrite = (List<string>)toWriteArray[0];               
        StreamWriter write = new StreamWriter("Selected Stocks.dat", true);          

        foreach(string s in toWrite)
        {
            write.WriteLine(s);      
        }
        write.Close();
    }
}

//Class to track what reports we want to include
public class LocalStocks
{
    public SelectedStocksObserver selectedStocks;
    public RecordCloseObserver recordClose;
    public AverageObserver averages;

    public LocalStocks()
    {
        selectedStocks = new SelectedStocksObserver();
        recordClose = new RecordCloseObserver();
        averages = new AverageObserver();
    }
}