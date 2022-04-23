using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatorController : MonoBehaviour
{

private enum ControlMode
    {
        /// <summary>
        /// Up moves the character forward, left and right turn the character gradually and down moves the character backwards
        /// </summary>
        Tank,
        /// <summary>
        /// Character freely moves in the chosen direction from the perspective of the camera
        /// </summary>
        Direct
    }

    [SerializeField] private float m_moveSpeed = 2;
    [SerializeField] private float m_turnSpeed = 2;
    [SerializeField] private float m_jumpForce = 20;

    [SerializeField] private Animator m_animator = null;
    [SerializeField] private Rigidbody m_rigidBody = null;
    // enumで定義したControlMode
    [SerializeField] private ControlMode m_controlMode = ControlMode.Direct;

    private float m_currentV = 0;
    private float m_currentH = 0;

    private readonly float m_interpolation = 10;
    private readonly float m_walkScale = 0.33f;
    private readonly float m_backwardsWalkScale = 0.16f;
    private readonly float m_backwardRunScale = 0.66f;

    private bool m_wasGrounded;
    private Vector3 m_currentDirection = Vector3.zero;

    private float m_jumpTimeStamp = 0;
    private float m_minJumpInterval = 0.25f;
    private bool m_jumpInput = false;

    private bool m_isGrounded;

    private List<Collider> m_collisions = new List<Collider>();

    private void Awake()
    {
        
        if (!m_animator) { gameObject.GetComponent<Animator>(); }
        if (!m_rigidBody) { gameObject.GetComponent<Animator>(); }
        
    }
// 他のコライダーにあたったときに呼ばれるOnCollisionEnter
// https://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
    private void OnCollisionEnter(Collision collision)
    {
        
        ContactPoint[] contactPoints = collision.contacts;
        // https://docs.unity3d.com/jp/current/ScriptReference/ContactPoint.html
        for (int i = 0; i < contactPoints.Length; i++)
        {
            // 接触点の法線normal
            // Vector3は、UnityEngineに含まれている構造体です。Unity全体の3Dでの座標や方向を表すために使用されます。
            if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
            {
                if (!m_collisions.Contains(collision.collider))
                {
                    m_collisions.Add(collision.collider);
                }
                m_isGrounded = true;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        ContactPoint[] contactPoints = collision.contacts;
        bool validSurfaceNormal = false;
        for (int i = 0; i < contactPoints.Length; i++)
        {
            if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
            {
                validSurfaceNormal = true; break;
            }
        }

        if (validSurfaceNormal)
        {
            m_isGrounded = true;
            // collisionのリストであるm_collisionsに OnCollisionStayの引数のコリジョンが入っているかをm_collisions.Containsで調べる
            if (!m_collisions.Contains(collision.collider))
            {
                m_collisions.Add(collision.collider);
            }
        }
        else
        {
            if (m_collisions.Contains(collision.collider))
            {
                m_collisions.Remove(collision.collider);
            }
            if (m_collisions.Count == 0) { m_isGrounded = false; }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (m_collisions.Contains(collision.collider))
        {
            m_collisions.Remove(collision.collider);
        }
        if (m_collisions.Count == 0) { m_isGrounded = false; }
    }

    private void Update()
    {
       
        if (!m_jumpInput && Input.GetKey(KeyCode.Space))
        {
            m_jumpInput = true;
        }
        
    }

// FixedUpdateは一定時間に呼ばれる
    private void FixedUpdate()
    {
       
        m_animator.SetBool("Grounded", m_isGrounded);

        switch (m_controlMode)
        {
            // 基本Directのモード
            case ControlMode.Direct:
                DirectUpdate();
                break;

            // case ControlMode.Tank:
            //     TankUpdate();
            //     break;

            default:
                Debug.LogError("Unsupported state");
                break;
        }

        m_wasGrounded = m_isGrounded;
        m_jumpInput = false;
        
    }

    private void TankUpdate()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");

        bool walk = Input.GetKey(KeyCode.LeftShift);

        if (v < 0)
        {
            if (walk) { v *= m_backwardsWalkScale; }
            else { v *= m_backwardRunScale; }
        }
        else if (walk)
        {
            v *= m_walkScale;
        }

        m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
        m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

        transform.position += transform.forward * m_currentV * m_moveSpeed * Time.deltaTime;
        transform.Rotate(0, m_currentH * m_turnSpeed * Time.deltaTime, 0);

        m_animator.SetFloat("MoveSpeed", m_currentV);

        JumpingAndLanding();
    }

    private void DirectUpdate()
    {
       
        // ↑」「↓」のことを「Vertical」
        float v = Input.GetAxis("Vertical");
        // GetAxisにおいては「→」「←」のことを「Horizontal」
        float h = Input.GetAxis("Horizontal");

// untaggedにして、名前をCameraに変えた
        // GameObject camera = transform.Find("Camera").gameObject;
        // Transform cameraTransform = camera.transform;
        
        // // もとのやつ
        Transform cameraTransform = Camera.main.transform;
        

        if (Input.GetKey(KeyCode.LeftShift))
        {
            v *= m_walkScale;
            h *= m_walkScale;
        }
        // 線形補間
        // Time.deltaTimeは基本的には約0.02秒
        // m_interpolationは10
        m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
        m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

        Vector3 direction = cameraTransform.forward * m_currentV + cameraTransform.right * m_currentH;
        // forwardは前後方向のz軸の値でほかは全て０のベクトル

        float directionLength = direction.magnitude;
        direction.y = 0;
        // 上方向への移動はないので０
        direction = direction.normalized * directionLength;
        // direction.normalizedで方向が同じの単位ベクトルにする

        if (direction != Vector3.zero)
        {
            // m_currentDirectionの初期値はゼロベクトル
            m_currentDirection = Vector3.Slerp(m_currentDirection, direction, Time.deltaTime * m_interpolation);
// https://qiita.com/hibit/items/d457ca6f9091fbb80ce6#:~:text=Quaternion.LookRotation()%20%E3%81%A8%E3%81%84%E3%81%86%E9%96%A2%E6%95%B0,%E4%BB%A3%E7%94%A8%E3%81%A8%E3%81%97%E3%81%A6%E3%81%BF%E3%81%BE%E3%81%97%E3%82%87%E3%81%86%E3%80%82
// 
            transform.rotation = Quaternion.LookRotation(m_currentDirection);
            // transform.positonはアタッチされているオブジェクトを指す
            transform.position += m_currentDirection * m_moveSpeed * Time.deltaTime;

            m_animator.SetFloat("MoveSpeed", direction.magnitude);
        }

        JumpingAndLanding();
            
        
    }

    private void JumpingAndLanding()
    {
        bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_minJumpInterval;

        if (jumpCooldownOver && m_isGrounded && m_jumpInput)
        {
            m_jumpTimeStamp = Time.time;
            m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
        }

        if (!m_wasGrounded && m_isGrounded)
        {
            m_animator.SetTrigger("Land");
        }

        if (!m_isGrounded && m_wasGrounded)
        {
            m_animator.SetTrigger("Jump");
        }
    }

}
