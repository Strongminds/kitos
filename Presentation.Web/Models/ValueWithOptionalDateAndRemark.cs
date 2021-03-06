﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Presentation.Web.Models
{
    public class ValueWithOptionalDateAndRemark<T>
    {
        public T Value { get; set; }

        public DateTime? OptionalDateValue { get; set; }

        public string Remark { get; set; }
    }
}