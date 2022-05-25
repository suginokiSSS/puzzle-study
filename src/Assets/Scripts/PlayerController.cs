using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
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

    Vector2Int _position;//���Ղ�̈ʒu
    RotState _rotate = RotState.Up;//�p�x��0�F��@1�F�E�@2�F���@3�F���Ŏ��i�q�Ղ�̈ʒu�j

    // Start is called before the first frame update
    void Start()
    {
        //�ЂƂ܂����ߑł��ŐF������
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

    private bool Translate(bool is_right)
    {
        //���z�I�Ɉړ��ł��邩���؂���
        Vector2Int pos = _position + (is_right ? Vector2Int.right : Vector2Int.left);
        if (!CanMove(pos, _rotate)) return false;

        //���ۂɈړ�
        _position = pos;

        _puyoControllers[0].SetPos(new Vector3((float)_position.x, (float)_position.y, 0.0f));
        Vector2Int posChild = CalcChildPuyoPos(_position, _rotate);
        _puyoControllers[1].SetPos(new Vector3((float)posChild.x, (float)posChild.y, 0.0f));

        return true;
    }

    bool Rotate(bool is_right)
    {
        RotState rot = (RotState)(((int)_rotate + (is_right ? +1 : +3)) & 3);

        //���z�I�Ɉړ��ł��邩���؂���i�㉺���E�ɂ��炵�������m�F�j
        Vector2Int pos = _position;
        switch (rot)
        {
            case RotState.Down:
                //�E�i���j���牺�F�����̉����E�i���j���Ƀu���b�N������Έ���������
                if(!boardController.CanSettle(pos + Vector2Int.down) || 
                   !boardController.CanSettle(pos + new Vector2Int(is_right ? 1 : -1, -1)))
                {
                    pos += Vector2Int.up;
                }
                break;
            case RotState.Right:
                //�E�F�E�����܂��Ă���΁A���Ɉړ�
                if (!boardController.CanSettle(pos + Vector2Int.right)) pos += Vector2Int.left;
                break;
            case RotState.Left:
                //���F�������܂��Ă���΁A�E�Ɉړ�
                if (!boardController.CanSettle(pos + Vector2Int.left)) pos += Vector2Int.right;
                break;
            case RotState.Up:
                break;
            default:
                Debug.Assert(false);
                break;
        }
        if (!CanMove(pos, rot)) return false;

        //���ۂɈړ�

        _rotate = rot;

        _puyoControllers[0].SetPos(new Vector3((float)_position.x, (float)_position.y, 0.0f));
        Vector2Int posChild = CalcChildPuyoPos(_position, _rotate);
        _puyoControllers[1].SetPos(new Vector3((float)posChild.x, (float)posChild.y, 0.0f));

        return true;
    }

    void QuickDrop()
    {
        //��������ԉ��܂ŗ�����
        Vector2Int pos = _position;
        do
        {
            pos += Vector2Int.down;
        } while (CanMove(pos, _rotate));
        pos -= Vector2Int.down;//���̏ꏊ�i�Ō�ɒu�����ꏊ�j�ɖ߂�

        _position = pos;

        //����ڒn
        bool is_set0 = boardController.Settle(_position,
            (int)_puyoControllers[0].GetPuyoType());
        Debug.Assert(is_set0);//�u�����̂͋󂢂Ă����ꏊ�̂͂�

        bool is_set1 = boardController.Settle(CalcChildPuyoPos(_position, _rotate),
            (int)_puyoControllers[1].GetPuyoType());
        Debug.Assert(is_set1);//�u�����̂͋󂢂Ă����ꏊ�̂͂�

        gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        //���s�ړ��̃L�[���͎擾
        if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            Translate(true);
        }
        if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Translate(false);
        }

        //��]�̃L�[���͎擾
        if(Input.GetKeyDown(KeyCode.X))
        {
            Rotate(true);
        }
        if(Input.GetKeyDown(KeyCode.Z))
        {
            Rotate(false);
        }

        //�N�C�b�N�h���b�v�̃L�[���͎擾
        if(Input.GetKey(KeyCode.UpArrow))
        {
            QuickDrop();
        }
    }
}
