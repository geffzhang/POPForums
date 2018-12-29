﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PopForums.Configuration;
using PopForums.Repositories;
using PopForums.Services;

namespace PopForums.AzureKit
{
    public static class ServiceCollectionExtensions
    {
	    public static IServiceCollection AddPopForumsRedisCache(this IServiceCollection services)
		{
			var serviceProvider = services.BuildServiceProvider();
			var config = serviceProvider.GetService<IConfig>();
			if (config.ForceLocalOnly)
				return services;
			services.Replace(ServiceDescriptor.Transient<ICacheHelper, PopForums.AzureKit.Redis.CacheHelper>());
		    return services;
	    }

		public static IServiceCollection AddPopForumsAzureSearch(this IServiceCollection services)
		{
			services.Replace(ServiceDescriptor.Transient<ISearchRepository, PopForums.AzureKit.Search.SearchRepository>());
			services.Replace(ServiceDescriptor.Transient<ISearchIndexSubsystem, PopForums.AzureKit.Search.SearchIndexSubsystem>());
			return services;
		}
	}
}
