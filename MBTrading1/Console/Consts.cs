using System;
using System.Configuration;

namespace MBTrading
{
    public static class Consts
    {
        // Program consts              
        public static int          MINUTE_CANDLES_PRIMARY;                     
        public static int          MINUTE_CANDLES_SECONDARY;                     
        public static int          NUM_OF_CANDLES;
        public static int          STOCHASTIC_PARAMETERS_LENGTH;
        public static int          STOCHASTIC_PARAMETERS_K_SMOOTHING_LENGTH;
        public static int          STOCHASTIC_PARAMETERS_D_SMOOTHING_LENGTH;
        public static int          RSI_PARAMETERS_LENGTH;
        public static int          WMA_PARAMETERS_LENGTH;
        public static int          esMA_PARAMETERS_LENGTH;
        public static double       esMA_PARAMETERS_PERCENTAGE;
        public static string       SYMBOLS_NAMES_FILE_PATH;                                                                            
        public static int          QUANTITY;
        public static int          AMOUNT_TO_RISK;
        public static int          AMOUNT_TO_BE_SATISFIED;
        public static double       PIPS_TO_PARTIAL;
        public static double       PIPS_TO_STOP_LOSS;
        public static double       PIPS_ABOVE_FOR_LIMIT_PRICE;
        public static double       PIPS_TO_STOP_LIMIT;
        public static double       JPY_PIPS_TO_PARTIAL;
        public static double       JPY_PIPS_TO_STOP_LOSS;
        public static double       JPY_PIPS_ABOVE_FOR_LIMIT_PRICE;
        public static double       JPY_PIPS_TO_STOP_LIMIT;

        // MBTradingUtils.cs Members - Fix Gateway
        public static bool         WorkOffLineMode;
        public static string       FixGW_TargetCompID;
        public static string       FixGW_SenderCompID;
        public static string       FixGW_Pass;
        public static int          FixGW_Port;
        public static string       FixGW_IP;
        public static string       Account_No;
        public static string       Account_UserName;
        public static string       Web_UserName;
        public static string       Web_Password;
        public static int          RolloverTime;
        public static int          SequenceResetTime;
   
        // Actions Members
        public static string       FilesPath;

        // NeuralNetwork
        public static int NEURAL_NETWORK_NUM_OF_TRAINING_CANDLES;
        public static int NEURAL_NETWORK_PROFIT_OR_LOSS_PIPS_RANGE;
        public static int NEURAL_NETWORK_CONST_CHANK_BETWEEN_NN_LEARNING;

        // WebTrading Server and DB
        public static string      WEBTRADING_SERVER_IP;
        public static int         WEBTRADING_SERVER_PORT;
        
        static Consts()
        {
            Consts.LoadConsts();
        }
        public static void LoadConsts()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);

            MINUTE_CANDLES_PRIMARY                      = int.Parse(ConfigurationManager.AppSettings["MINUTE_CANDLES_PRIMARY"]);
            MINUTE_CANDLES_SECONDARY                    = int.Parse(ConfigurationManager.AppSettings["MINUTE_CANDLES_SECONDARY"]);
            STOCHASTIC_PARAMETERS_LENGTH                = int.Parse(ConfigurationManager.AppSettings["STOCHASTIC_PARAMETERS_LENGTH"]);
            STOCHASTIC_PARAMETERS_K_SMOOTHING_LENGTH    = int.Parse(ConfigurationManager.AppSettings["STOCHASTIC_PARAMETERS_K_SMOOTHING_LENGTH"]);
            STOCHASTIC_PARAMETERS_D_SMOOTHING_LENGTH    = int.Parse(ConfigurationManager.AppSettings["STOCHASTIC_PARAMETERS_D_SMOOTHING_LENGTH"]);
            RSI_PARAMETERS_LENGTH                       = int.Parse(ConfigurationManager.AppSettings["RSI_PARAMETERS_LENGTH"]);
            WMA_PARAMETERS_LENGTH                       = int.Parse(ConfigurationManager.AppSettings["WMA_PARAMETERS_LENGTH"]);
            esMA_PARAMETERS_LENGTH                      = int.Parse(ConfigurationManager.AppSettings["esMA_PARAMETERS_LENGTH"]);
            esMA_PARAMETERS_PERCENTAGE                  = double.Parse(ConfigurationManager.AppSettings["esMA_PARAMETERS_PERCENTAGE"]);
            NUM_OF_CANDLES                              = Math.Max(Math.Max(STOCHASTIC_PARAMETERS_LENGTH, RSI_PARAMETERS_LENGTH), esMA_PARAMETERS_LENGTH) + 1;

            SYMBOLS_NAMES_FILE_PATH             = ConfigurationManager.AppSettings["SYMBOLS_NAMES_FILE_PATH"];
            QUANTITY                            = int.Parse(ConfigurationManager.AppSettings["QUANTITY"]);
            AMOUNT_TO_RISK                      = 0 - (int)(QUANTITY * ((double)int.Parse(ConfigurationManager.AppSettings["PERCENTAGE_FROM_QUANTITY_TO_RISK"]) / 100.0));
            AMOUNT_TO_BE_SATISFIED              =     (int)(QUANTITY * (double.Parse(ConfigurationManager.AppSettings["PERCENTAGE_TO_PROFIT_PROPORTION_TO_RISK"]) / 100.0));
            PIPS_TO_PARTIAL                     = double.Parse(ConfigurationManager.AppSettings["PIPS_TO_PARTIAL"]);
            PIPS_TO_STOP_LOSS                   = double.Parse(ConfigurationManager.AppSettings["PIPS_TO_STOP_LOSS"]);
            PIPS_ABOVE_FOR_LIMIT_PRICE          = double.Parse(ConfigurationManager.AppSettings["PIPS_ABOVE_FOR_LIMIT_PRICE"]);
            PIPS_TO_STOP_LIMIT                  = double.Parse(ConfigurationManager.AppSettings["PIPS_TO_STOP_LIMIT"]);
            JPY_PIPS_TO_PARTIAL                 = double.Parse(ConfigurationManager.AppSettings["JPY_PIPS_TO_PARTIAL"]);
            JPY_PIPS_TO_STOP_LOSS               = double.Parse(ConfigurationManager.AppSettings["JPY_PIPS_TO_STOP_LOSS"]);
            JPY_PIPS_ABOVE_FOR_LIMIT_PRICE      = double.Parse(ConfigurationManager.AppSettings["JPY_PIPS_ABOVE_FOR_LIMIT_PRICE"]);
            JPY_PIPS_TO_STOP_LIMIT              = double.Parse(ConfigurationManager.AppSettings["JPY_PIPS_TO_STOP_LIMIT"]);


            WorkOffLineMode     = bool.Parse(ConfigurationManager.AppSettings["WorkOffLineMode"]);
            FixGW_TargetCompID  = ConfigurationManager.AppSettings["FixGW_TargetCompID"];
            FixGW_SenderCompID  = ConfigurationManager.AppSettings["FixGW_SenderCompID"];
            FixGW_Pass          = ConfigurationManager.AppSettings["FixGW_Pass"];
            FixGW_Port          = int.Parse(ConfigurationManager.AppSettings["FixGW_Port"]);
            FixGW_IP            = ConfigurationManager.AppSettings["FixGW_IP"];
            Account_No          = ConfigurationManager.AppSettings["Account_No"];
            Account_UserName    = ConfigurationManager.AppSettings["Account_UserName"];
            Web_UserName        = ConfigurationManager.AppSettings["Web_UserName"];
            Web_Password        = ConfigurationManager.AppSettings["Web_Password"];
            RolloverTime        = int.Parse(ConfigurationManager.AppSettings["RolloverTime"]);
            SequenceResetTime   = int.Parse(ConfigurationManager.AppSettings["SequenceResetTime"]);


            FilesPath                               = ConfigurationManager.AppSettings["FilesPath"];


            NEURAL_NETWORK_NUM_OF_TRAINING_CANDLES          = int.Parse(ConfigurationManager.AppSettings["NEURAL_NETWORK_NUM_OF_TRAINING_CANDLES"]);
            NEURAL_NETWORK_PROFIT_OR_LOSS_PIPS_RANGE        = int.Parse(ConfigurationManager.AppSettings["NEURAL_NETWORK_PROFIT_OR_LOSS_PIPS_RANGE"]);
            NEURAL_NETWORK_CONST_CHANK_BETWEEN_NN_LEARNING  = int.Parse(ConfigurationManager.AppSettings["NEURAL_NETWORK_CONST_CHANK_BETWEEN_NN_LEARNING"]);
            WEBTRADING_SERVER_IP                    = ConfigurationManager.AppSettings["WEBTRADING_SERVER_IP"];
            WEBTRADING_SERVER_PORT                  = int.Parse(ConfigurationManager.AppSettings["WEBTRADING_SERVER_PORT"]);
        }
    }
}