# MoonDriverDotNET  
 MoonDriver��.NET�łł��B  
   
[�T�v]  
  MoonDriver��.NET�łɈڐA�������̂ł��B  
  
[�@�\�A����]  
 MoonDriver�̃R���p�C���A�h���C�o�̋@�\���g�p�ł��܂��B  
  
[�K�v�Ȋ�]  
 �EWindows7�ȍ~��OS���C���X�g�[�����ꂽPC  
 �E�e�L�X�g�G�f�B�^  
 �E�C���ƍ���  
  
[���g�p�̑O��]  
 �A�[�J�C�u�ɓ�������Ă���removeZoneIdent.bat�����s���A�]�[�����ʎq���폜���Ă��������B  
 (�]�[�����ʎq�Ƃ͈Ӑ}�����Ƀ_�E�����[�h�����v���O���������s�����ۂɁA�����}�����邽�߂Ƀt�@�C���ɒǉ������A  
 �Z�L�����e�B�Ɋւ�����ł��B�Ӑ}�����_�E�����[�h�ł����Ă��t������܂��̂œ���Ɏx�Ⴊ����ꍇ������܂��B)  
  
[�N�C�b�N�X�^�[�g]  
  �R���p�C��  
    ������compile.bat��mml�t�@�C��(�g���q��.mdl�𐄏�)���h���b�v���ăR���p�C�����s���܂��B  
  ���t  
    ������play.bat��mdr�t�@�C�����h���b�v���ĉ��t���s���܂��B  
  �I�v�V�����Ȃǂ̎w��͏�L��bat�t�@�C����ҏW���Ďw�肵�Ă��������B  
  (�������R�}���h���C�����璼�ڎw�肷�邱�Ƃ��ł��܂��B)  
  
[�I���W�i���łƈقȂ�@�\]  
  �R���p�C��  
�@�@�EPCMPACK�Ɋւ���@�\  

        �R���p�C�����̃I�v�V����  
            -PCMPACK �I�v�V���� PCM���w�肵���t�@�C������PACK���܂��B  
                ����)  
                -PCMPACK:pcmFileName
                    pcmFileName  PCM�t�@�C��  
                �g�p��)  
                    -PCMPACK:TEST.PCM  
  
        .mdl�t�@�C������mml�Ƃ��Ẵ^�O�w��  
            #PCMFILE �^�O    �g�p����PCM�t�@�C�����w�肵�܂��B  
                �g�p��)  
                    #PCMFILE TEST.PCM  
                �ڍ�)  
                    ���̃^�O���w�肵��.mdl�t�@�C���͎w�肵��PCM�t�@�C�����g�p���邱�Ƃ�錾���܂��B  
                    �R���p�C���ɂ���Đ������ꂽ.mdr�t�@�C���ɁAPCMPACK�@�\�ɂ����PCM�𓯍����Ȃ������ꍇ�́A  
                    ���t���Ƀh���C�o�����̃t�@�C�������Q�Ƃ��A�t�@�C����ǂݍ��݂܂��B  
                    ��q��#PCMPACK�^�O�ɂ����PCM�������w�肵���ꍇ�́A  
                    �R���p�C�����ɃR���p�C�������̃t�@�C�������Q�Ƃ��A�t�@�C����ǂݍ���.mdr�t�@�C���ɓ������܂��B  
                    ���AMoonDriverDotNET�ł�PCMPACK���R���p�C���I�v�V�����Ƃ��Ă��Ă��邱�Ƃ��\�ł��B  
                    ���̏ꍇ�̓I�v�V�����Ŏw�肵���t�@�C������PCM�t�@�C���Ƃ��Ďg�p���܂��B  
  
            #PCMPACK �^�O    PCM��PACK���邩�ǂ����w�肵�܂��B  
                ����)  
                    #PCMPACK [ ON | OFF ]  
                        ON PACK����  
                        OFF PACK���Ȃ�(�K��l)  
                �g�p��)  
                    #PCMPACK ON  
                �ڍ�)  
                    ���̃^�O���w�肵��.mdl�t�@�C����PCM�t�@�C���𓯍�(PACK)���邩�ǂ������w�肵�܂��B  
                    ON���w�肷�邱�Ƃœ������܂��BOFF�͓������܂���B  
                    �������w�肷�邱�ƂŁA�R���p�C���͎w�肵��PCM�t�@�C���̓��e(PCM�f�[�^)��.mdr�t�@�C���ɒǉ����܂��B  
                    ���t���A�h���C�o��.mdr�t�@�C���ɓ�������PCM�f�[�^���g�p����悤�ɂȂ�A�V����PCM��ǂݍ��݂܂���B  
                    ���AMoonDriverDotNET�ł�PCMPACK���R���p�C���I�v�V�����Ƃ��Ă��Ă��邱�Ƃ��\�ł��B  
                    ���̏ꍇ�̓I�v�V�������D�悳��܂��B  
  
        �I�v�V�����A#PCMFILE�A#PCMPACK�̎w��D�揇��
            -PCMPACK�I�v�V�������w�肷��ƁA�R���p�C����#PCMFILE�A#PCMPACK�^�O�𖳎����܂��B  
        
  
[���쌠�E�Ɛ�]  
MoonDriverDotNET��MIT���C�Z���X�Ƃ��܂��B  
���쌠�͍�҂��ۗL���Ă��܂��B  
���̃\�t�g�͖��ۏ؂ł���A���̃\�t�g���g�p�������ɂ��  
�����Ȃ鑹�Q����҂͈�؂̐ӔC�𕉂��܂���B  
  
�ȉ��̃\�t�g�E�F�A�̃\�[�X�R�[�h��C#�����ɉ��ς��g�p���Ă��܂��B  
���̓R�[�h/dll���g�p�����Ă��������Ă���܂��B  
�����̃\�[�X/�o�C�i���͊e����҂����쌠�������܂��B  
���C�Z���X�Ɋւ��ẮA�e�h�L�������g���Q�Ƃ��Ă��������B  
  
 �Emoon_driver/mmckc    -> ?               -> �R�[�h�Q�l�A�ڐA�A����  
 �EmusicDriverInterface -> MIT             -> dll���I�����N�Ŏg�p  
  
  
[SpecialThanks]  
 �{�c�[���͈ȉ��̕��X�ɂ����b�ɂȂ��Ă���܂��B�܂��ȉ��̃\�t�g�E�F�A�A�E�F�u�y�[�W���Q�l�A�g�p���Ă��܂��B  
 �E�ڂ����� ����  
 �EManbow-J ����  
 �E��݂���� ����  

 �Emoon_driver/mmckc  
 �EVisual Studio Community 2019  
 �E������G�f�B�^�[  
 �ENAUDIO  
 �E��R�̑f���炵�����y�f�[�^���тɂ��̃v���O���}�̕��X  

