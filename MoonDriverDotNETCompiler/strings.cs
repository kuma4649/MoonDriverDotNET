using System;
using System.Collections.Generic;
using System.Text;

namespace MoonDriverDotNET.Compiler
{
    public class strings
    {
        //		/************************************************************/
        //		/*															*/
        //		/************************************************************/
        //# include	<stddef.h>
        //# include	<ctype.h>
        //# include	<stdio.h>
        //# include	<stdlib.h>
        //# include	<string.h>


        /*--------------------------------------------------------------
            スペース／タブのスキップ
         Input:
            char	*ptr		:データ格納ポインタ
         Output:
            char	*ptr		;スキップ後のポインタ
        --------------------------------------------------------------*/
        public int skipSpaceOld(string buf, int ptr)
        {
            while (ptr < buf.Length && buf[ptr] != '\0')
            {
                if (buf[ptr] != ' ' && buf[ptr] != '\t')
                {
                    break;
                }
                ptr++;
            }
            return ptr;
        }


        /*--------------------------------------------------------------
            文字列のスキップ
        --------------------------------------------------------------*/

        public int skipQuote(string buf, int ptr)
        {
            if (buf[ptr] != 0 &&
                    buf[ptr] == '\"')
            {
                ptr++; // skip start charactor
                while (ptr < buf.Length && buf[ptr] != 0)
                {
                    if (buf[ptr] == '\"') // end of the quote
                    {
                        ptr++; break;
                    }

                    if (buf[ptr] == '\\' && buf[ptr + 1] != 0) // skip Escape
                        ptr++;
                    ptr++;
                }
            }
            return ptr;
        }

        /*--------------------------------------------------------------
            コメント文字のチェック
        --------------------------------------------------------------*/
        public bool isComment(string buf, int ptr)
        {
            if (buf[ptr] != 0 &&
                    (buf[ptr] == ';' ||
                     //		 (*ptr == '/' && *(ptr+1) == '/')
                     buf[ptr] == '/'))
                return true;

            return false;
        }

        /*--------------------------------------------------------------
            コメントのスキップ
        --------------------------------------------------------------*/
        public int skipComment(string buf, int ptr)
        {
            if (isComment(buf, ptr))
            {
                while (true)
                {
                    // '\0' = EOL or EOF , '\n' = EOL
                    if (ptr==buf.Length || buf[ptr] == '\0' || buf[ptr] == '\n')
                        break;
                    ptr++;
                }
            }
            return ptr;
        }


        /*--------------------------------------------------------------
			スペース／タブのスキップ(行コメントも飛ばす)
		--------------------------------------------------------------*/
        public int skipSpace(string buf, int ptr)
        {
            while (true)
            {
                if (ptr == buf.Length) break; //EOL or EOF
                if (buf[ptr] == ' ' || buf[ptr] == '\t')
                {
                    //Skip Space
                    ptr++;
                    continue;
                }
                else if (isComment(buf, ptr))
                {
                    //Skip Comment(return EOL)
                    ptr = skipComment(buf, ptr);
                }
                else
                {
                    //Normal Chars
                    break;
                }
            }
            return ptr;
        }






        //		/*----------------------------------------------------------
        //			文字が漢字かどうかのチェック
        //		 Input:
        //			char	c	: 文字
        //		 Return:
        //			0:漢字以外 1: 漢字コード
        //		----------------------------------------------------------*/
        //		int checkKanji(unsigned char c)
        //		{
        //			if (0x81 <= c && c <= 0x9f) return 1;
        //			if (0xe0 <= c && c <= 0xef) return 1;
        //			return 0;
        //		}



        //		/*----------------------------------------------------------
        //			文字列を大文字にする(漢字対応版)
        //		 Input:
        //			char *ptr	: 文字列へのポインタ
        //		 Output:
        //			none
        //		----------------------------------------------------------*/
        //		void strupper(char* ptr)
        //		{
        //			while (*ptr != '\0')
        //			{
        //				if (checkKanji((unsigned char) * ptr) == 0 ) {
        //					*ptr = toupper((int)*ptr);
        //					ptr++;
        //				} else
        //				{
        //					/* 漢字の時の処理 */
        //					ptr += 2;
        //				}
        //			}
        //		}




        /*--------------------------------------------------------------
            文字列を数値に変換
         Input:

         Output:

        --------------------------------------------------------------*/
        public int Asc2Int(string buf, int ptr, ref int cnt)
        {
            int num;
            char c;
            int minus_flag = 0;

            num = 0;
            cnt = 0;

            if (buf[ptr] == '-')
            {
                minus_flag = 1;
                ptr++;
                cnt++;
            }
            switch (buf[ptr])
            {
                /* 16進数 */
                case 'x':
                case '$':
                    ptr++;
                    cnt++;
                    while (true)
                    {
                        c = buf[ptr].ToString().ToUpper()[0];
                        if ('0' <= c && c <= '9')
                        {
                            num = num * 16 + (c - '0');
                        }
                        else if ('A' <= c && c <= 'F')
                        {
                            num = num * 16 + (c - 'A' + 10);
                        }
                        else
                        {
                            break;
                        }
                        cnt++;
                        ptr++;
                    }
                    break;
                /* 2進数 */
                case '%':
                    ptr++;
                    cnt++;
                    while (true)
                    {
                        if ('0' <= buf[ptr] && buf[ptr] <= '1')
                        {
                            num = num * 2 + (buf[ptr] - '0');
                        }
                        else
                        {
                            break;
                        }
                        cnt++;
                        ptr++;
                    }
                    break;
                /* 10進数 */
                default:
                    while (true)
                    {
                        if ('0' <= buf[ptr] && buf[ptr] <= '9')
                        {
                            num = num * 10 + (buf[ptr] - '0');
                        }
                        else
                        {
                            break;
                        }
                        cnt++;
                        ptr++;
                    }
                    break;
            }
            if (minus_flag != 0)
            {
                num = -num;
            }
            return num;
        }
    }
}
