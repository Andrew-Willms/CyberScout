using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ScoutingApp.AppManagement;



public class AppManagerGetter {

	public AppManager? Instance { get; private set; }

	public async Task<AppManager?> Create() {

		if (Instance is not null) {
			return null; // TODO: turn this into an error
		}

		Instance = await AppManager.Create();
		return Instance; // TODO: pass on errors from this function which still need to be implemented
	}
}



public class AppManagerNotCreatedException : Exception {

	public required Type CallingClass { get; init; }

	[SetsRequiredMembers]
	public AppManagerNotCreatedException(Type callerClass) {

		CallingClass = callerClass;
	}

	public AppManagerNotCreatedException() { }

}