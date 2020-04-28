using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration.Annotations;
using CsvHelper;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Framework.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Commands;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;
using Sitecore.Services.Examples.SynchronizeCatalog.Policies;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Blocks
{
    [PipelineDisplayName("GetModelsFromFileBlock")]
    public class GetModelsFromFileBlock : PipelineBlock<SynchronizeCatalogArgument, SynchronizeCatalogArgument, CommercePipelineExecutionContext>
    {
        private readonly IMapper _mapper;

        public GetModelsFromFileBlock(IMapper mapper)
        {
            _mapper = mapper;
        }

        public override async Task<SynchronizeCatalogArgument> Run(SynchronizeCatalogArgument arg, CommercePipelineExecutionContext context)
        {
            var policy = context.GetPolicy<SynchronizeCatalogPolicy>();

            arg.Products =  await ProcessFileType<Product>(policy.SourceFolderLocation, policy.SuccessFolderLocation, context);
            arg.Variants = await ProcessFileType<Variant>(policy.SourceFolderLocation, policy.SuccessFolderLocation, context);
            arg.Categories = await ProcessFileType<Category>(policy.SourceFolderLocation, policy.SuccessFolderLocation, context);
            arg.Catalogs = await ProcessFileType<Catalog>(policy.SourceFolderLocation, policy.SuccessFolderLocation, context);

            arg.Options.ExcludeLogInResults = policy.ExcludeLogInResults;
            arg.Options.SkipRelationships = policy.SkipRelationships;

            return arg;
        }

        public async Task<List<T>> ProcessFileType<T>(string sourceFolder, string passFolder, CommercePipelineExecutionContext context)
        {
            var path = $@"{sourceFolder}\{typeof(T).Name.ToLower()}.csv";
            var pathPass = $@"{passFolder}\{typeof(T).Name.ToLower()}{Guid.NewGuid()}.csv";
            var moveFile = false;

            var resultList = new List<T>();
            if (!File.Exists(path))
            {
                return resultList;
            }

            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {

                try
                {
                    csv.Configuration.HasHeaderRecord = true;
                    var records = csv.GetRecords<dynamic>().ToList();
                    var convertedList = _mapper.Map<List<T>>(records);
                    resultList = convertedList;
                    moveFile = true;
                }
                catch (Exception e)
                {
                    moveFile = false;
                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            "FileCouldNotBeLoaded",
                            new object[] { e.Message },
                            $"Could not load file'{path}").ConfigureAwait(false),
                        context);
                    return null;
                }
            }

            if (moveFile)
                File.Move(path, pathPass);

            return resultList;
        }

       
    }
}
