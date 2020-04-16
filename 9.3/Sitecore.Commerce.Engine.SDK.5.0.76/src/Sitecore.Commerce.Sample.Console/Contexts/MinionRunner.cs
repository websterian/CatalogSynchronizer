using System;
using System.Collections.Generic;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Sample.Console;

namespace Sitecore.Commerce.Sample.Contexts
{
    public class MinionRunner
    {
        public MinionRunner()
        {
            Context = new ShopperContext
            {
                Shop = Program.DefaultStorefront,
                ShopperId = "MinionRunnerShopperId",
                Language = "en-US",
                Currency = "USD",
                PolicyKeys = "ZeroMinionDelay|xActivityPerf",
                EffectiveDate = DateTimeOffset.Now,
                Components = new List<Component>()
            };
        }

        public ShopperContext Context { get; set; }
    }
}
