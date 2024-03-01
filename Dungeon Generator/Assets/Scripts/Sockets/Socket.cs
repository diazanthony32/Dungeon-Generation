using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class Socket : MonoBehaviour
{
    // Add a menu item to create custom GameObjects.
    // Priority 10 ensures it is grouped with the other menu items of the same kind
    // and propagated to the hierarchy dropdown and hierarchy context menus.
    [MenuItem("GameObject/Tiles/Socket", false, 10)]
    static void CreateCustomGameObject(MenuCommand menuCommand)
    {
        // Create a custom game object
        var newGO = Resources.Load("Socket/Socket"); ;
        var socket = PrefabUtility.InstantiatePrefab(newGO);
        socket.name = "Socket";

        StageUtility.PlaceGameObjectInCurrentStage((GameObject)socket);

        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign((GameObject)socket, menuCommand.context as GameObject);

        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(socket, "Create " + socket.name);

        Selection.activeObject = socket;
    }

    //Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(transform.position, -transform.forward);

        Vector3 right = Quaternion.LookRotation(-transform.forward) * Quaternion.Euler(0, 180 + 20.0f, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(-transform.forward) * Quaternion.Euler(0, 180 - 20.0f, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(transform.position + -transform.forward, right * 0.25f);
        Gizmos.DrawRay(transform.position + -transform.forward, left * 0.25f);

    }
}
