using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using Fusion.XR.Shared.Core;
using UnityEngine.Events;

namespace Fusion.Addons.Tools
{
    /// <summary>
    /// Track an activated state. 
    /// Take the state authority to store the activation state only if it is not currently activated (if the current state authority is activating, they can only stop the activation)
    /// </summary>
    public class NetworkedActivator : NetworkBehaviour
    {
        [Networked, OnChangedRender(nameof(OnActivatedChange))]
        public NetworkBool Activated {get; set;}

        // We only allow to take the authority during an activation of the activator is not activated: otherwise, we want the curretn state authority to handle the desactivation
        public bool CanActivate => Object.HasStateAuthority || Activated == false || Object.IsStateAuthorityPresent() == false;
        public bool CanDesactivate => (Object.HasStateAuthority || Object.IsStateAuthorityPresent() == false) && Activated;

        public UnityEvent onActivate = new UnityEvent();
        public UnityEvent onDesactivate = new UnityEvent();

        public bool desactivateIfStateAuthorityLeave = true;

        public override void Spawned()
        {
            base.Spawned();
            NotifyState();
        }

        public override async void Render()
        {
            base.Render();
            if (desactivateIfStateAuthorityLeave && Activated && Object.IsStateAuthorityPresent() == false)
            {
                // We ensure that one player recovers the state auth
                await Object.EnsureHasStateAuthority();
                // If we are the "elected" new state auth, we switch Activated off
                if (Object.HasStateAuthority)
                {
                    TryDesactivate();
                }
            }
        }

        void OnActivatedChange()
        {
            NotifyState();
        }

        void NotifyState()
        {
            if (Activated)
            {
                onActivate.Invoke();
            }
            else
            {
                onDesactivate.Invoke();
            }
        }

        public async void TryActivate()
        {
            if (CanActivate)
            {
                if (Object.HasStateAuthority == false)
                {
                    await Object.WaitForStateAuthority();
                }
                Activated = true;
            }
        }

        public async void TryDesactivate()
        {
            if (CanDesactivate)
            {
                if (Object.HasStateAuthority == false)
                {
                    await Object.WaitForStateAuthority();
                }
                Activated = false;
            }
        }

        public void TryToggle()
        {
            if (Activated)
            {
                TryDesactivate();
            }
            else
            {
                TryActivate();
            }
        }
    }
}


