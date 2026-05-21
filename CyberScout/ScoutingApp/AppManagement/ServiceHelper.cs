using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace ScoutingApp.AppManagement; 



public static class ServiceHelper {

	public static T GetService<T>() => Current.GetService<T>() ?? throw new($"Could not resolve service {typeof(T).Name}");

	private static IServiceProvider Current => IPlatformApplication.Current.Services;

}