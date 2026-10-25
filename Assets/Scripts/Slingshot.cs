using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slingshot : MonoBehaviour
{
    // fields set in the Unity Inspector pane
    [Header("Inscribed")]
    public GameObject projectilePrefab;
    public float velocityMult = 10f;
    public GameObject projLinePrefab;
    public LineRenderer rubberBand;
    public Transform bandAnchorLeft;
    public Transform bandAnchorRight;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip launchSound;
    public AudioSource stretchAudioSource;
    public AudioClip stretchSound;

    // fields set dynamically
    [Header("Dynamic")]
    public GameObject launchPoint;
    public Vector3 launchPos;
    public GameObject projectile;
    public bool aimingMode;

    void Awake() {
        Transform launchPointTrans = transform.Find("LaunchPoint");
        launchPoint = launchPointTrans.gameObject;
        launchPoint.SetActive(false);
        launchPos = launchPointTrans.position;
        
        rubberBand.positionCount = 3;
        rubberBand.enabled = false;
    }

    void OnMouseEnter() {
        launchPoint.SetActive(true);
    }

    void OnMouseExit() {
        launchPoint.SetActive(false);
    }

    void OnMouseDown() {
        if (aimingMode || 
            MissionDemolition.GAME_OVER || 
            MissionDemolition.SHOTS_LEFT() <= 0 ||
            Goal.goalMet){
            return;
        }

        // The player has pressed the mouse button while over Slingshot
        aimingMode = true;
        // Instantiate a Projectile
        projectile = Instantiate(projectilePrefab) as GameObject;
        // Start it at the launchPoint
        projectile.transform.position = launchPos;
        // Set it to isKinematic for now
        projectile.GetComponent<Rigidbody>().isKinematic = true;
        // Enable the rubber band line renderer
        rubberBand.enabled = true;
        //start the stretch sound
        StartStretchSound();
    }

    void StartStretchSound() {
        if(stretchAudioSource == null || stretchSound == null) {
            return;
        }

        stretchAudioSource.clip = stretchSound;
        stretchAudioSource.loop = true;
        stretchAudioSource.Play();
    }
    void StopStretchSound() {
        if(stretchAudioSource != null && stretchAudioSource.isPlaying) {
            stretchAudioSource.Stop();
        }
    }

    void Update() {
        // If Slingshot is not in aimingMode, don't run this code
        if(!aimingMode) return;

        // Get the current mouse position in 2D screen coordinates
        Vector3 mousePos2D = Input.mousePosition;
        mousePos2D.z = -Camera.main.transform.position.z;
        Vector3 mousePos3D = Camera.main.ScreenToWorldPoint(mousePos2D);

        // Find the delta from the launchPos to the mousePos3D
        Vector3 mouseDelta = mousePos3D - launchPos;
        // Limit mouseDelta to the radius of the Slingshot SphereCollider
        float maxMagnitude = this.GetComponent<SphereCollider>().radius;
        if(mouseDelta.magnitude > maxMagnitude) {
            mouseDelta.Normalize();
            mouseDelta *= maxMagnitude;
        }
        // Move the projectile to this new position
        Vector3 projPos = launchPos + mouseDelta;
        projectile.transform.position = projPos;

        // Update the rubber band positions to match the projectile and anchors
        rubberBand.SetPosition(0, bandAnchorLeft.position);
        rubberBand.SetPosition(1, projectile.transform.position);
        rubberBand.SetPosition(2, bandAnchorRight.position);

        //check if goal was hit
        if(Goal.goalMet) {
            aimingMode = false;
            StopStretchSound();

            if(rubberBand != null)
                rubberBand.enabled = false;

            return;
        }

        if(Input.GetMouseButtonUp(0)) {
            // The monuse has been released
            aimingMode = false;
            rubberBand.enabled = false;

            //Stop the stretch sound
            StopStretchSound();

            // Play the launch sound
            if(audioSource != null && launchSound != null) {
                audioSource.PlayOneShot(launchSound);
            }   

            Rigidbody projRB = projectile.GetComponent<Rigidbody>();
            projRB.isKinematic = false;
            projRB.collisionDetectionMode = CollisionDetectionMode.Continuous;
            projRB.velocity = -mouseDelta * velocityMult;

            // Switch to slingshot view immediately before setting POI
            FollowCam.SWITCH_VIEW(FollowCam.eView.slingshot);
            
            FollowCam.POI = projectile; // Set the _MainCamera POI
            Instantiate<GameObject>(projLinePrefab, projectile.transform);
            projectile = null;
            MissionDemolition.SHOT_FIRED();
        }
    }
}
