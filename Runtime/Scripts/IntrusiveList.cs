using System;

namespace Miscreant.Utilities.Lifecycle
{
	internal abstract class IntrusiveList
	{
		public CustomUpdateBehaviour head = null;
		public uint count = 0;

		protected CustomUpdateBehaviour current;

		public void ExecuteAll()
		{
			if (count == 0)
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

		public void Traverse(Action<CustomUpdateBehaviour> perElementAction)
		{
			if (count == 0)
			{
				return;
			}

			current = head;
			do
			{
				perElementAction.Invoke(current);
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