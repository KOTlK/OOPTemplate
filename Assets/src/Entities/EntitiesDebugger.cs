using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ArrayUtils;

public class EntitiesDebugger : MonoBehaviour {
    public EntityManager EntityManager;
    public Camera        DebugCamera;
    public Camera        PreviousCamera;
    public float         CameraDefaultSpeed = 5f;
    public float         CameraFastSpeed    = 15f;
    public float         CameraSensitivity  = 120f; // angle per second
    public float         RollSensitivity    = 50f;
    public bool          Enabled = false;
    public bool          SortEntities = false;
    public KeyCode       KeyToEnable = KeyCode.BackQuote;
    public EntityType    SortByType;
    public float         EntitiesListWidth = 0.15f;
    public float         EntityHeight = 0.06f;
    public float         DescriptionWidth = 0.30f;

    public float FieldWidth  = 0.20f;
    public float FieldHeight = 0.045f;
    public float AdditionalFieldOffset = 0.1f;
    public float AdditionalFieldWidth  = 0.2f;
    public float AdditionalFieldHeight = 0.035f;
    public float VectorInputWidth = 0.07f;

    public int    ItemsPerScroll = 1;
    public uint   FirstEntity;
    public uint   FirstField;
    public uint[] SortedEntities = new uint[1024];
    public uint   SortedEntitiesCount;
    public uint   SelectedEntity;
    public uint   PreviousEntity;

    private void Awake() {
        DebugCamera.gameObject.SetActive(false);
    }

    private void Update() {
        if(Input.GetKeyDown(KeyToEnable)) {
            Enabled = !Enabled;
            if(Enabled) {
                PreviousCamera = Camera.main;
                Camera.main.gameObject.SetActive(false);
                DebugCamera.gameObject.SetActive(true);
            } else {
                PreviousCamera.gameObject.SetActive(true);
                DebugCamera.gameObject.SetActive(false);
            }
        }

        if(Enabled) {
            //Rotate and move camera
            var t = DebugCamera.transform;
            var z = Input.GetAxisRaw("Vertical");
            var x = Input.GetAxisRaw("Horizontal");
            var y = 0f;
            var forward = t.forward;
            var right   = t.right;
            var up  = t.up;
            var speedUpCamera = Input.GetKey(KeyCode.LeftShift);
            var cameraSpeed = speedUpCamera ? CameraFastSpeed : CameraDefaultSpeed;
            var mousex = Input.GetAxisRaw("Mouse X") * CameraSensitivity * Time.deltaTime;
            var mousey = -Input.GetAxisRaw("Mouse Y") * CameraSensitivity * Time.deltaTime;
            var mousez = 0f;
            var currentRotation = t.rotation;

            if(Input.GetKey(KeyCode.Q)) {
                mousez += 1f;
            }

            if(Input.GetKey(KeyCode.E)) {
                mousez -= 1f;
            }

            mousez *= RollSensitivity * Time.deltaTime;
            
            var inputRotation = Quaternion.Euler(mousey, mousex, mousez);
            currentRotation *= inputRotation;

            t.rotation = currentRotation;

            if(Input.GetKey(KeyCode.Space)) {
                y += 1f;
            }

            if(Input.GetKey(KeyCode.LeftControl)) {
                y -= 1f;
            }


            var direction = new Vector3(x, y, z).normalized * cameraSpeed * Time.deltaTime;
            forward *= direction.z;
            right   *= direction.x;
            up      *= direction.y;

            t.position += forward + right + up;
        }
    }

    private void OnGUI() {
        if(Enabled) {
            var screenHeight = Screen.height;
            var screenWidth  = Screen.width;
            var entityWidth  = screenWidth * EntitiesListWidth;
            var entityHeight = screenHeight * EntityHeight;
            var fieldWidth   = screenWidth * FieldWidth;
            var fieldHeight  = screenHeight * FieldHeight;
            var currentEntitiesListHeight = screenHeight * 0.01f;
            var descriptionWidth = screenWidth * DescriptionWidth;
            var descriptionStart = screenWidth - descriptionWidth;
            var additionalFieldOffset = descriptionWidth * AdditionalFieldOffset;
            var additionalFieldWidth  = screenWidth * AdditionalFieldWidth;
            var additionalFieldHeight = screenHeight * AdditionalFieldHeight;
            var mousePosition    = Input.mousePosition;

            ClearSortedEntities();

            GUI.Box(new Rect(0, 0, screenWidth, screenHeight), "Entities Debugger");

            var entitiesListRect = new Rect(0, 0, screenWidth * EntitiesListWidth, screenHeight);

            GUI.BeginGroup(entitiesListRect); //Entities List

            GUI.BeginGroup(new Rect(0, currentEntitiesListHeight, screenWidth * EntitiesListWidth, screenHeight)); // Sorted Entities

            for(uint i = 0; i < EntityManager.MaxEntitiesCount; ++i) {
                if(currentEntitiesListHeight > screenHeight)
                    break;

                if(EntityManager.Entities[i].Alive) {
                    if(!SortEntities) {
                        PushEntity(i);
                    } else {
                        if(EntityManager.Entities[i].Type == SortByType) {
                            PushEntity(i);
                        }
                    }
                }
            }

            if(entitiesListRect.Contains(mousePosition)) {
                var mouseDelta = (Math.Sign(Input.mouseScrollDelta.y) * ItemsPerScroll);
                var firstEntity = FirstEntity - mouseDelta;

                if(firstEntity < 0) {
                    FirstEntity = 0;
                } else if(firstEntity >= SortedEntitiesCount) {
                    FirstEntity = SortedEntitiesCount - 1;
                } else {
                    FirstEntity = (uint)firstEntity;
                }
            }

            for(var i = FirstEntity; i < SortedEntitiesCount; ++i) {
                if(GUI.Button(new Rect(0, currentEntitiesListHeight, entityWidth, entityHeight), 
                              $"{EntityManager.Entities[SortedEntities[i]].Type}")) {
                    PreviousEntity = SelectedEntity;
                    SelectedEntity = SortedEntities[i];
                }

                currentEntitiesListHeight += entityHeight;
            }

            GUI.EndGroup(); //Sorted Entities

            GUI.EndGroup(); //Entities List

            if(SelectedEntity != 0) {
                var descriptionRect =
                    new Rect(descriptionStart, 0, descriptionWidth, screenHeight);
                GUI.BeginGroup(descriptionRect);
                 // Entity Description

                GUI.Box(new Rect(0, 0, descriptionWidth, screenHeight), $"{EntityManager.Entities[SelectedEntity].Type}:{SelectedEntity}");

                var entity = EntityManager.Entities[SelectedEntity].Entity;
                var type = entity.GetType();
                var members = type.GetFields(BindingFlags.Instance  |
                                            BindingFlags.Public    |
                                            BindingFlags.NonPublic)
                                  .Cast<MemberInfo>()
                                  .Concat(type.GetProperties(BindingFlags.Instance |
                                               BindingFlags.Public   |
                                               BindingFlags.NonPublic)).ToArray();
                uint membersCount = (uint)members.Length;
                
                var currentFieldHeight = screenHeight * 0.05f;
                
                if(descriptionRect.Contains(mousePosition)) {
                    var mouseDelta = Math.Sign(Input.mouseScrollDelta.y) * ItemsPerScroll;
                    var firstField = FirstField - mouseDelta;

                    if(firstField < 0) {
                        FirstField = 0;
                    } else if(firstField >= membersCount) {
                        FirstField = membersCount - 1;
                    } else {
                        FirstField = (uint)firstField;
                    }
                }

                for (var i = FirstField; i < membersCount; ++i) {
                    var member = members[i];
                    if (member.GetCustomAttributes<HideInInspector>().Any()) {
                        continue;
                    }
                    
                    if(IsUnityField(member))
                        continue;

                    DisplayMember(member, 
                                  entity, 
                                  ref currentFieldHeight, 
                                  fieldWidth, 
                                  fieldHeight,
                                  additionalFieldOffset,
                                  additionalFieldWidth,
                                  additionalFieldHeight);
                    currentFieldHeight += fieldHeight;

                    if (currentFieldHeight > screenHeight)
                        break;
                }

                GUI.EndGroup(); //Entity Description
            }
        }
    }

    private void PushEntity(uint entity) {
        if(SortedEntitiesCount == SortedEntities.Length) {
            Resize(ref SortedEntities, SortedEntitiesCount << 1);
        }

        SortedEntities[SortedEntitiesCount++] = entity;
    }

    private void ClearSortedEntities() {
        SortedEntitiesCount = 0;
    }

    private string px = "0";
    private string py = "0";
    private string pz = "0";
    private string rx = "0";
    private string ry = "0";
    private string rz = "0";
    private string sx = "0";
    private string sy = "0";
    private string sz = "0";

    private void DisplayMember(MemberInfo member, 
                               Entity entity, 
                               ref float currentHeight, 
                               float width, 
                               float height,
                               float additionalFieldOffset,
                               float additionalFieldWidth,
                               float additionalFieldHeight) {
        GUI.Label(new Rect(0, currentHeight, width, height), member.Name);

        switch(member.Name) {
            case "transform": {
                var t = (Transform)((PropertyInfo)member).GetValue(entity.transform);
                var offset = additionalFieldOffset;
                var position = t.position;
                var rotation = t.rotation.eulerAngles;
                var scale    = t.localScale;

                px = position.x.ToString();
                py = position.y.ToString();
                pz = position.z.ToString();
                rx = rotation.x.ToString();
                ry = rotation.y.ToString();
                rz = rotation.z.ToString();
                sx = scale.x.ToString();
                sy = scale.y.ToString();
                sz = scale.z.ToString();
                
                //Draw position
                currentHeight += additionalFieldHeight;

                GUI.Label(new Rect(offset, currentHeight, width, height), "Position");
                currentHeight += additionalFieldHeight;
                offset += additionalFieldOffset;

                DrawVector3Input(ref px,
                                 ref py,
                                 ref pz,
                                 ref currentHeight,
                                 offset,
                                 additionalFieldHeight);
                
                offset -= additionalFieldOffset;

                //Draw rotation
                currentHeight += additionalFieldHeight;

                GUI.Label(new Rect(offset, currentHeight, width, height), "Rotation");
                currentHeight += additionalFieldHeight;
                offset += additionalFieldOffset;

                DrawVector3Input(ref rx,
                                 ref ry,
                                 ref rz,
                                 ref currentHeight,
                                 offset,
                                 additionalFieldHeight);

                offset -= additionalFieldOffset;

                //Draw scale
                currentHeight += additionalFieldHeight;

                GUI.Label(new Rect(offset, currentHeight, width, height), "LocalScale");
                currentHeight += additionalFieldHeight;
                offset += additionalFieldOffset;

                DrawVector3Input(ref sx,
                                 ref sy,
                                 ref sz,
                                 ref currentHeight,
                                 offset,
                                 additionalFieldHeight);

                FormatTransform();

                t.position = new Vector3(float.Parse(px), 
                                         float.Parse(py), 
                                         float.Parse(pz));

                t.rotation = Quaternion.Euler(float.Parse(rx),
                                              float.Parse(ry),
                                              float.Parse(rz));

                t.localScale = new Vector3(float.Parse(sx),
                                           float.Parse(sy),
                                           float.Parse(sz));
            }
            break;
        }
    }

    private void DrawVector3Input(ref string x, 
                                  ref string y, 
                                  ref string z,
                                  ref float currentHeight,
                                  float offset,
                                  float height) {
        var width = Screen.width * VectorInputWidth;

        x = GUI.TextField(new Rect(offset,
                                   currentHeight,
                                   width,
                                   height), x);

        currentHeight += height;

        y = GUI.TextField(new Rect(offset,
                                   currentHeight,
                                   width,
                                   height), y);
        
        currentHeight += height;

        z = GUI.TextField(new Rect(offset,
                                   currentHeight,
                                   width,
                                   height), z);
    }


    // =)
    private void FormatTransform() {
        if(px.EndsWith('.') || px.EndsWith(',')) {
            px.Remove(px.Length - 1);
            px += '1';
        }

        if(py.EndsWith('.') || py.EndsWith(',')) {
            py.Remove(py.Length - 1);
            py += '1';
        }

        if(pz.EndsWith('.') || pz.EndsWith(',')) {
            pz.Remove(pz.Length - 1);
            pz += '1';
        }

        if(sx.EndsWith('.') || sx.EndsWith(',')) {
            sx.Remove(sx.Length - 1);
            sx += '1';
        }

        if(sy.EndsWith('.') || sy.EndsWith(',')) {
            sy.Remove(sy.Length - 1);
            sy += '1';
        }

        if(sz.EndsWith('.') || sz.EndsWith(',')) {
            sz.Remove(sz.Length - 1);
            sz += '1';
        }

        if(rx.EndsWith('.') || rx.EndsWith(',')) {
            rx.Remove(rx.Length - 1);
            rx += '1';
        }

        if(ry.EndsWith('.') || ry.EndsWith(',')) {
            ry.Remove(ry.Length - 1);
            ry += '1';
        }

        if(rz.EndsWith('.') || rz.EndsWith(',')) {
            rz.Remove(rz.Length - 1);
            rz += '1';
        }
    }

    private static bool IsUnityField(MemberInfo field) {
        return field.Name switch {
            "destroyCancellationToken" => true,
            "useGUILayout" => true,
            "runInEditMode" => true,
            "allowPrefabModeInPlayMode" => true,
            "isActiveAndEnabled" => true,
            "gameObject" => true,
            "tag" => true,
            "rigidbody" => true,
            "rigidbody2D" => true,
            "camera" => true,
            "light" => true,
            "animation" => true,
            "constantForce" => true,
            "renderer" => true,
            "audio" => true,
            "networkView" => true,
            "collider" => true,
            "collider2D" => true,
            "hingeJoint" => true,
            "particleSystem" => true,
            "hideFlags" => true,
            _ => false
        };
    }
}