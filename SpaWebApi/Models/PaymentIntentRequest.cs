﻿using System.ComponentModel.DataAnnotations;
using BuildEntities;

namespace SpaWebApi.Models
{
    public class PaymentIntentRequest : VideoBuildRequest
    {
        [Required]
        public License License { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Cost { get; set; }
    }
}