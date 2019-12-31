using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Lifecycle
{
	internal sealed class IntrusiveFixedUpdateList : IntrusiveList
	{
		protected override void ExecuteCurrent()
		{
			current.ManagedFixedUpdate();
		}

		protected override void Advance()
		{
			current = current.nextFixedUpdate;
		}

		internal override void AddToTail(CustomUpdateBehaviour component)
		{
			if (!ReferenceEquals(component.nextFixedUpdate, null) || !ReferenceEquals(component.previousFixedUpdate, null))
			{
				return;
			}
			count++;

			if (ReferenceEquals(head, null))
			{
				head = component;
				head.previousFixedUpdate = head;
				head.nextFixedUpdate = head;
				return;
			}

			Add(component, head.previousFixedUpdate, head);
		}

		private void Add(CustomUpdateBehaviour node, CustomUpdateBehaviour prev, CustomUpdateBehaviour next)
		{
			node.nextFixedUpdate = next;
			node.previousFixedUpdate = prev;

			next.previousFixedUpdate = node;
			prev.nextFixedUpdate = node;
		}

		internal override void Remove(CustomUpdateBehaviour component)
		{
			if (ReferenceEquals(component.nextFixedUpdate, null) && ReferenceEquals(component.previousFixedUpdate, null))
			{
				return;
			}
			count--;

			if (count == 0)
			{
				// TODO: Miscreant: Restore this under a "strict" conditional
				// if (!ReferenceEquals(component, head))
				// {
				// 	throw new ArgumentException("component is not the root of the list");
				// }

				// TODO: MIscreant: Elegantly handle "current" field  when removing the last element in the list. Need to work out semantics there, too. 

				head.previousFixedUpdate = null;
				head.nextFixedUpdate = null;
				head = null;
				return;
			}

			if (ReferenceEquals(component, head))
			{
				head = head.nextFixedUpdate;
			}

			component.nextFixedUpdate.previousFixedUpdate = component.previousFixedUpdate;
			component.previousFixedUpdate.nextFixedUpdate = component.nextFixedUpdate;

			if (ReferenceEquals(current, component))
			{
				current = component.previousFixedUpdate;
			}

			component.previousFixedUpdate = null;
			component.nextFixedUpdate = null;
		}
	}
}