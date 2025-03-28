﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerLibrary.Models
{
    public class PrizeModel
    {
        public int Id { get; set; }
        public int PlaceNumber { get; set; }

        public string PlaceName { get; set; }

        public decimal PrizeAmount { get; set; }

        public double PrizePercentage { get; set; }

        public PrizeModel()
        {

        }
        public PrizeModel( string placeNumber, string placeName, string prizeAmount, string prizePercentage)
        {
            PlaceName = placeName;

            int placeNumberValue = 0;
            int.TryParse(placeNumber, out placeNumberValue);
            PlaceNumber = placeNumberValue;

            decimal PrizeAmountValue = 0;
            decimal.TryParse(prizeAmount, out PrizeAmountValue);
            PrizeAmount = PrizeAmountValue;

            double prizePercentageValue = 0;
            double.TryParse(prizeAmount, out prizePercentageValue);
            PrizePercentage = prizePercentageValue;
        }
    }
}
