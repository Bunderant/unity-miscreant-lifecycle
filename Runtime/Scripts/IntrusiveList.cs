using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	internal abstract class IntrusiveList
	{
		public readonly CustomUpdatePriority priorityGroup;
		public CustomUpdateBehaviour head = null;
		public uint count = 0;

		protected CustomUpdateBehaviour current;

		public IntrusiveList(CustomUpdatePriority priorityGroup)
		{
			this.priorityGroup = priorityGroup;
		}

		public void ExecuteAll()
		{
			if (ReferenceEquals(head, null))
			{
				return;
			}

			current = head;
			do
			{
				ExecuteCurrent();
				Advance();
			} while (!ReferenceEquals(current, head));
			current = null;
		}

		protected abstract void ExecuteCurrent();
		protected abstract void Advance();

		internal abstract void AddToTail(CustomUpdateBehaviour component);
		internal abstract void Remove(CustomUpdateBehaviour component);
	}
}