using Cinemachine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    public class CameraController : MonoBehaviour
    {
        // State
        CinemachineStateDrivenCamera stateCamera = null;
        List<CinemachineVirtualCamera> virtualCameras = new List<CinemachineVirtualCamera>();

        // Cached References
        Player player = null;
        Party party = null;

        private void Awake()
        {
            stateCamera = GetComponentInChildren<CinemachineStateDrivenCamera>();
            virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>().ToList();
            SetupPlayerReferences();
        }

        private void SetupPlayerReferences()
        {
            GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");
            if (playerGameObject != null)
            {
                player = playerGameObject.GetComponent<Player>();
                party = player.GetComponent<Party>();
            }
        }

        private void Start()
        {
            if (player != null)
            {
                SetUpStateDrivenCamera();
                SetUpVirtualCameraFollowers();
            }
        }

        private void SetUpStateDrivenCamera()
        {
            UpdateStateAnimator(party.GetLeadCharacterAnimator());
        }

        private void SetUpVirtualCameraFollowers()
        {
            foreach (CinemachineVirtualCamera virtualCamera in virtualCameras)
            {
                virtualCamera.Follow = player.transform;
            }
        }

        private void UpdateStateAnimator(Animator characterAnimator)
        {
            stateCamera.m_AnimatedTarget = characterAnimator;
        }
    }

}
