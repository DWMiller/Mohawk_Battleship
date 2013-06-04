﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBC.Core.mbc.accolade
{
    public interface AccoladeProcessor
    {
        /**
         * <summary>Process an activity</summary>
         */
        RoundLog.RoundAccolade Process(RoundLog.RoundActivity a);
    }
}
