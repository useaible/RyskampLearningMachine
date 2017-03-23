using Newtonsoft.Json;
using RLM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LogisticsGameLib
{
    public class LogisticSimulator
    {
        public Timer newDayTimer;
        public Timer orderGeneratorTimer;

        private Player retailer;
        private Player wholeSaler;
        private Player distributor;
        private Player factory;

        private double storageCost;
        private double backlogCost;
        private int retailerInitInv;
        private int wholesalerInitInv;
        private int distributorInitInv;
        private int factoryInitInv;

        private int currentDay = 0;
        private IEnumerable<int> CustomerOrders;

        private static Random rand = new Random();
        private System.Threading.CancellationTokenSource cancelTokenSrc = new System.Threading.CancellationTokenSource();

        public IEnumerable<TFOutput> TFOutput { get; set; }

        public int NoDaysInterval { get; set; } = 2000; //2 seconds elapse per day
        
        public LogisticSimOutput SimulationOutput { get; private set; } = new LogisticSimOutput();

        public int CurrentDay
        {
            get { return currentDay; }
        }

        public LogisticSimulator(double storageCost, double backlogCost, int retailerInitInv, int wholesalerInitInv, int distributorInitInv, int factoryInitInv)
        {
            this.storageCost = storageCost;
            this.backlogCost = backlogCost;

            this.retailerInitInv = retailerInitInv;
            this.wholesalerInitInv = wholesalerInitInv;
            this.distributorInitInv = distributorInitInv;
            this.factoryInitInv = factoryInitInv;
        }
        
        public static IEnumerable<int> GenerateCustomerOrders()
        {
            var customerOrders = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                customerOrders.Add(rand.Next(1, 30));
            }

            return customerOrders;
        }

        public void ResetSimulationOutput()
        {
            SimulationOutput = new LogisticSimOutput();
        }

        public void StartNewDay(System.Threading.CancellationToken token, int delay = 50)
        {      
            while (CurrentDay < 100)
            {
                if (token.IsCancellationRequested)
                    break;

                currentDay++;
                var simOutputDay = new LogisticSimOutputDay() { Day = currentDay };

                // retailer's customers
                GetNewOrder();

                // clear previous day's transactions
                this.retailer.ClearTransactions();
                wholeSaler.ClearTransactions();
                this.distributor.ClearTransactions();
                this.factory.ClearTransactions();
                                
                // accept new shipments
                this.retailer.ProcessIncomingShipments();
                wholeSaler.ProcessIncomingShipments();
                this.distributor.ProcessIncomingShipments();
                this.factory.ProcessIncomingShipments();

                // process backlog and current orders
                this.retailer.ProcessOrders();
                wholeSaler.ProcessOrders();
                this.distributor.ProcessOrders();
                this.factory.ProcessOrders();

                // if necessary, order new stock to replenish inventory
                this.retailer.Ordering();
                wholeSaler.Ordering();
                this.distributor.Ordering();
                this.factory.Ordering();


                dynamic retailer = new
                {
                    Inventory = this.retailer.Inventory- this.retailer.Backlogs.Sum(a => a.Amount),
                    Name = this.retailer.Name,
                    Expected = this.retailer.Expected,
                    Shipped = this.retailer.CurSend,
                    Ordered = this.retailer.Ordered,
                    StorageCost = this.retailer.StorageCostTotal,
                    BacklogCost = this.retailer.BacklogCostTotal
                };

                dynamic wholesaler = new
                {
                    Inventory = wholeSaler.Inventory-wholeSaler.Backlogs.Sum(a => a.Amount),
                    Name = wholeSaler.Name,
                    Expected = wholeSaler.Expected,
                    Shipped = wholeSaler.CurSend,
                    Ordered = wholeSaler.Ordered,
                    StorageCost = wholeSaler.StorageCostTotal,
                    BacklogCost = wholeSaler.BacklogCostTotal,
                };

                dynamic distributor = new
                {
                    Inventory = this.distributor.Inventory- this.distributor.Backlogs.Sum(a => a.Amount),
                    Name = this.distributor.Name,
                    Expected = this.distributor.Expected,
                    Shipped = this.distributor.CurSend,
                    Ordered = this.distributor.Ordered,
                    StorageCost = this.distributor.StorageCostTotal,
                    BacklogCost = this.distributor.BacklogCostTotal
                };

                dynamic factory = new
                {
                    Inventory = this.factory.Inventory- this.factory.Backlogs.Sum(a => a.Amount),
                    Name = this.factory.Name,
                    Expected = this.factory.Expected,
                    Shipped = this.factory.CurSend,
                    Ordered = this.factory.Ordered,
                    StorageCost = this.factory.StorageCostTotal,
                    BacklogCost = this.factory.BacklogCostTotal
                };

                List<dynamic> playerDetails = new List<dynamic> { retailer, wholesaler, distributor, factory };
                List<dynamic> orders = new List<dynamic>();
                orders.AddRange(this.retailer.Transactions);
                orders.AddRange(wholeSaler.Transactions);
                orders.AddRange(this.distributor.Transactions);
                orders.AddRange(this.factory.Transactions);
                
                simOutputDay.PlayerDetails = playerDetails;
                simOutputDay.Orders = orders;
                SimulationOutput.SimulatedDays.Add(simOutputDay);
            }
        }

        //place a new order to the retailer orders
        public void GetNewOrder()
        {
            
            int amountToOrder = CustomerOrders.ElementAt(CurrentDay - 1);
            var ord = new Order("Flavored Beer", amountToOrder) { ProcessOn = CurrentDay };

            retailer.Orders.Enqueue(ord);
            retailer.Ordered = ord.Amount;
        }
        public void NextStepInitialization()
        {
            //todo: determine if an agent/player needs to order and set distributor curReceive and curSend to 0
            //then set all agents/players Ordered to 0
        }
        
        /// <summary>
        /// Starts the simulation
        /// </summary>
        /// <param name="outputs">This is where final results was stored after the game.</param>
        /// <param name="delay">Specified speed of the simulation.</param>
        /// <param name="customOrders">The list of orders by a customer.</param>
        public void Start(IEnumerable<LogisticSimulatorOutput> outputs, int delay = 50, IEnumerable<int> customOrders = null)
        {
            currentDay = 0;
            if (customOrders != null)
                CustomerOrders = customOrders;

            int retailerMin = Convert.ToInt32(outputs.First(x => x.Name == "Retailer_Min").Value);
            int retailerMax = Convert.ToInt32(outputs.First(x => x.Name == "Retailer_Max").Value);
            int wholeSalerMin = Convert.ToInt32(outputs.First(x => x.Name == "WholeSaler_Min").Value);
            int wholeSalerMax = Convert.ToInt32(outputs.First(x => x.Name == "WholeSaler_Max").Value);
            int distributorMin = Convert.ToInt32(outputs.First(x => x.Name == "Distributor_Min").Value);
            int distributorMax = Convert.ToInt32(outputs.First(x => x.Name == "Distributor_Max").Value);
            int factoryMin = Convert.ToInt32(outputs.First(x => x.Name == "Factory_Min").Value);
            int factoryMax = Convert.ToInt32(outputs.First(x => x.Name == "Factory_Max").Value);
            int factoryUnitsPerDay = Convert.ToInt32(outputs.First(x => x.Name == "Factory_Units_Per_Day").Value);

            retailer = new Player(this, "Retailer", storageCost, backlogCost, retailerInitInv, retailerMax, retailerMin, 2, 2, null);
            wholeSaler = new Player(this, "WholeSaler", storageCost, backlogCost, wholesalerInitInv, wholeSalerMax, wholeSalerMin, 2, 2, null);
            distributor = new Player(this, "Distributor", storageCost, backlogCost, distributorInitInv, distributorMax, distributorMin, 2, 2, null);
            factory = new Player(this, "Factory", storageCost, backlogCost, factoryInitInv, factoryMax, factoryMin, 2, 2, factoryUnitsPerDay);

            retailer.Right = wholeSaler;

            wholeSaler.Left = retailer;
            wholeSaler.Right = distributor;

            distributor.Left = wholeSaler;
            distributor.Right = factory;

            factory.Left = distributor;

            StartNewDay(cancelTokenSrc.Token, delay);

            SimulationOutput.Score = SumAllCosts() * -1;
        }

        /// <summary>
        /// Gets the total of all costs after simulation.
        /// </summary>
        /// <returns>Double: The total of all costs.</returns>
        public double SumAllCosts()
        {
            double retVal = 0;

            retVal = retailer.StorageCostTotal + retailer.BacklogCostTotal + wholeSaler.StorageCostTotal + wholeSaler.BacklogCostTotal + distributor.StorageCostTotal + distributor.BacklogCostTotal + factory.StorageCostTotal + factory.BacklogCostTotal;

            retVal = retVal * -1;
            return retVal;
        }

        /// <summary>
        /// Stops the simulation.
        /// </summary>
        public void Stop()
        {
            cancelTokenSrc.Cancel();
        }
    }
}
