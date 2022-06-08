using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    const int TRANS_TIME = 3;//移動速度遷移時間
    const int ROT_TIME = 3;//回転遷移時間
    enum RotState
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,

        Invalid = -1,
    }

    [SerializeField] PuyoController[] _puyoControllers = new PuyoController[2] { default!, default! };
    [SerializeField] BoardController boardController = default!;

    Vector2Int _position;//軸ぷよの位置
    RotState _rotate = RotState.Up;//角度は0：上　1：右　2：下　3：左で持つ（子ぷよの位置）

    AnimationController _animationController = new AnimationController();
    Vector2Int _last_position;
    RotState _last_rotate = RotState.Up;

    Logicallnput logicallnput = new ();
    // Start is called before the first frame update
    void Start()
    {
        //ひとまず決め打ちで色を決定
        _puyoControllers[0].SetPuyoType(PuyoType.Green);
        _puyoControllers[1].SetPuyoType(PuyoType.Red);

        _position = new Vector2Int(2, 12);
        _rotate = RotState.Up;

        _puyoControllers[0].SetPos(new Vector3((float)_position.x, (float)_position.y, 0.0f));
        Vector2Int posChild = CalcChildPuyoPos(_position, _rotate);
        _puyoControllers[1].SetPos(new Vector3((float)_position.x, (float)_position.y + 1.0f, 0.0f));
    }
    static readonly Vector2Int[] rotate_tbl = new Vector2Int[]
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };
    private static Vector2Int CalcChildPuyoPos(Vector2Int pos, RotState rot)
    {
        return pos + rotate_tbl[(int)rot];
    }
    private bool CanMove(Vector2Int pos, RotState rot)
    {
        if (!boardController.CanSettle(pos)) return false;
        if (!boardController.CanSettle(CalcChildPuyoPos(pos, rot))) return false;

        return true;
    }

    void SetTransition(Vector2Int pos, RotState rot, int time)
    {
        //補間のために保存しておく
        _last_position = _position;
        _last_rotate = _rotate;

        //値の更新
        _position = pos;
        _rotate = rot;

        _animationController.Set(time);
    }

    private bool Translate(bool is_right)
    {
        //仮想的に移動できるか検証する
        Vector2Int pos = _position + (is_right ? Vector2Int.right : Vector2Int.left);
        if (!CanMove(pos, _rotate)) return false;

        //実際に移動
        SetTransition(pos, _rotate, TRANS_TIME);

        return true;
    }

    bool Rotate(bool is_right)
    {
        RotState rot = (RotState)(((int)_rotate + (is_right ? +1 : +3)) & 3);

        //仮想的に移動できるか検証する（上下左右にずらした時も確認）
        Vector2Int pos = _position;
        switch (rot)
        {
            case RotState.Down:
                //右（左）から下：自分の下か右（左）下にブロックがあれば引きあがる
                if(!boardController.CanSettle(pos + Vector2Int.down) || 
                   !boardController.CanSettle(pos + new Vector2Int(is_right ? 1 : -1, -1)))
                {
                    pos += Vector2Int.up;
                }
                break;
            case RotState.Right:
                //右：右が埋まっていれば、左に移動
                if (!boardController.CanSettle(pos + Vector2Int.right)) pos += Vector2Int.left;
                break;
            case RotState.Left:
                //左：左が埋まっていれば、右に移動
                if (!boardController.CanSettle(pos + Vector2Int.left)) pos += Vector2Int.right;
                break;
            case RotState.Up:
                break;
            default:
                Debug.Assert(false);
                break;
        }
        if (!CanMove(pos, rot)) return false;

        //実際に移動
        SetTransition(pos, rot, ROT_TIME);
        
        return true;
    }

    void QuickDrop()
    {
        //落ちれる一番下まで落ちる
        Vector2Int pos = _position;
        do
        {
            pos += Vector2Int.down;
        } while (CanMove(pos, _rotate));
        pos -= Vector2Int.down;//一つ上の場所（最後に置けた場所）に戻す

        _position = pos;

        //直後接地
        bool is_set0 = boardController.Settle(_position,
            (int)_puyoControllers[0].GetPuyoType());
        Debug.Assert(is_set0);//置いたのは空いていた場所のはず

        bool is_set1 = boardController.Settle(CalcChildPuyoPos(_position, _rotate),
            (int)_puyoControllers[1].GetPuyoType());
        Debug.Assert(is_set1);//置いたのは空いていた場所のはず

        gameObject.SetActive(false);
    }

    static readonly KeyCode[] key_code_tbl = new KeyCode[(int)Logicallnput.Key.MAX]
    {
        KeyCode.RightArrow,
        KeyCode.LeftArrow,
        KeyCode.X,
        KeyCode.Z,
        KeyCode.UpArrow,
        KeyCode.DownArrow,
    };

    //入力を取り込む
    void UpdateInput()
    {
        Logicallnput.Key inputDev = 0;

        //キー入力取得
        for (int i = 0; i < (int)Logicallnput.Key.MAX; i++)
        {
            if(Input.GetKey(key_code_tbl[i]))
            {
                inputDev |= (Logicallnput.Key)(1 << i);
            }
        }

        logicallnput.Update(inputDev);
    }

    void Control()
    {
        //平行移動のキー入力取得
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Translate(true);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Translate(false);
        }

        //回転のキー入力取得
        if (Input.GetKeyDown(KeyCode.X))
        {
            Rotate(true);
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Rotate(false);
        }

        //クイックドロップのキー入力取得
        if (Input.GetKey(KeyCode.UpArrow))
        {
            QuickDrop();
        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //入力を取り込む
        UpdateInput();

        //操作を受けて動かす
        if(!_animationController.Update(Time.deltaTime))//アニメ中はキー入力を受け付けない
        {
            Control();
        }

        //表示
        float anim_rate = _animationController.GetNormalized();
        _puyoControllers[0].SetPos(Interpolate(_position, RotState.Invalid, _last_position, RotState.Invalid, anim_rate));
        _puyoControllers[1].SetPos(Interpolate(_position, _rotate, _last_position, _last_rotate, anim_rate));
    }

    //rateが1 -> 0 で、pos_last -> rot に遷移、rot_last -> rot に遷移、rot が RotState.Invalidなら回転を考慮しない（軸ぷよ用）
    static Vector3 Interpolate(Vector2Int pos, RotState rot, Vector2Int pos_last, RotState rot_last, float rate)
    {
        //平行移動
        Vector3 p = Vector3.Lerp(
            new Vector3((float)pos.x, (float)pos.y, 0.0f),
            new Vector3((float)pos_last.x, (float)pos_last.y, 0.0f), rate);

        if (rot == RotState.Invalid) return p;

        //回転
        float theta0 = 0.5f * Mathf.PI * (float)(int)rot;
        float theta1 = 0.5f * Mathf.PI * (float)(int)rot_last;
        float theta = theta1 - theta0;

        //近い方向に回る
        if (+Mathf.PI < theta) theta = theta - 2.0f * Mathf.PI;
        if (theta < -Mathf.PI) theta = theta + 2.0f * Mathf.PI;

        theta = theta0 + rate * theta;

        return p + new Vector3(Mathf.Sin(theta), Mathf.Cos(theta), 0.0f);
    }
}
