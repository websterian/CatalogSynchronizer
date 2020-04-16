using System;
using System.Collections.Generic;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Sample.Console;

namespace Sitecore.Commerce.Sample.Contexts
{
    public class MinionRunnerHab
    {
        public MinionRunnerHab()
        {
            Context = new ShopperContext
            {
                Shop = Program.DefaultStorefront,
                ShopperId = "MinionRunnerShopperId",
                Language = "en-US",
                Currency = "USD",
                PolicyKeys = "ZeroMinionDelay|xActivityPerf",
                Environment = "HabitaMinions",
                EffectiveDate = DateTimeOffset.Now,
                Components = new List<Component>()
            };
        }

        public ShopperContext Context { get; set; }
    }
}
