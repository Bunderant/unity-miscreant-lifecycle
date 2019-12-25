using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	[CreateAssetMenu(menuName = nameof(Miscreant) + "/" + nameof(Miscreant.Utilities.Lifecycle) + "/" + nameof(CustomUpdatePriority))]
	public sealed class CustomUpdatePriority : ScriptableObject
	{
		public int Index { get; private set; }

		/// <summary>
		/// Should only ever be called by a <see cref="CustomUpdateManager" /> ScriptableObject asset. 
		/// Always use a the manager object to control execution order. 
		/// </summary>
		/// <param name="index">The new priority value.</param>
		internal void SetIndex(int index, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
		{
			// TODO: Miscreant: Need to make sure the calling object is the right type, as well. 
			if (!string.Equals(callerName, nameof(CustomUpdateManager.UpdatePriorities)))
			{
				Debug.LogError(
					$"Can't set priority outside the {nameof(CustomUpdateManager.UpdatePriorities)} " +
					$"method of {nameof(CustomUpdateManager)}",
					this
				);
			}

			this.Index = index;
		}
	}
}

