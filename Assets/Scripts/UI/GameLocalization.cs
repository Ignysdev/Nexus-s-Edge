public enum GameText
{
    // Menu
    MenuTitle,
    MenuCredits,
    MenuPlay,
    MenuOptions,
    MenuQuit,
    MenuBack,
    MenuSkipTutorial,
    MenuMusicVolume,
    MenuSfxVolume,
    MenuSensitivity,
    MenuLanguage,
    MenuPortuguese,
    MenuEnglish,

    // Tutorial - passos
    TutorialStep1Title,
    TutorialStep1Body,
    TutorialStep1Hint,
    TutorialStep2Title,
    TutorialStep2Body,
    TutorialStep2Hint,
    TutorialStep3Title,
    TutorialStep3Body,
    TutorialStep3Hint,
    TutorialStep4Title,
    TutorialStep4Body,
    TutorialStep4Hint,
    TutorialStep5Title,
    TutorialStep5Body,
    TutorialStep5Hint,

    // Tutorial - prompts
    TutorialFinalTitle,
    TutorialFinalBody,
    TutorialFinalHint,
    TutorialBreakFlowTitle,
    TutorialBreakFlowBody,
    TutorialBreakFlowHint,
    TutorialSpecialTitle,
    TutorialSpecialBody,
    TutorialSpecialHint,

    // Tutorial - falhas
    TutorialFailEvadeInsteadParry,
    TutorialFailParryInsteadDodge,
    TutorialFailBlocked,
    TutorialFailHitDodge,
    TutorialFailHitParry,
    TutorialFailUseHeavy,
    TutorialFailBreakFlow,

    // Tutorial - conclusão
    TutorialCompleteTitle,
    TutorialCompleteBody,
    TutorialCompleteHint,

    // Overlays
    VictoryTitle,
    VictoryBody,
    DefeatTitle,
    DefeatBody,
    PauseTitle,
    PauseBody,
    Resume,
    Restart,
    Quit
}

static public class GameLocalization
{
    static public event System.Action LanguageChanged;

    static public string Get(GameText i_text)
    {
        bool i_english = GameSettings.Language == 1;

        switch (i_text)
        {
            case GameText.MenuTitle:
                return "NEXUS'S EDGE";
            case GameText.MenuCredits:
                return i_english ? "Game by IgnysDev" : "Jogo por IgnysDev";
            case GameText.MenuPlay:
                return i_english ? "PLAY" : "JOGAR";
            case GameText.MenuOptions:
                return i_english ? "OPTIONS" : "OPÇÕES";
            case GameText.MenuQuit:
                return i_english ? "QUIT" : "SAIR";
            case GameText.MenuBack:
                return i_english ? "BACK" : "VOLTAR";
            case GameText.MenuSkipTutorial:
                return i_english ? "SKIP TUTORIAL" : "PULAR TUTORIAL";
            case GameText.MenuMusicVolume:
                return i_english ? "MUSIC VOLUME" : "VOLUME DA MÚSICA";
            case GameText.MenuSfxVolume:
                return i_english ? "SFX VOLUME" : "VOLUME DOS EFEITOS";
            case GameText.MenuSensitivity:
                return i_english ? "SENSITIVITY" : "SENSIBILIDADE";
            case GameText.MenuLanguage:
                return i_english ? "LANGUAGE" : "IDIOMA";
            case GameText.MenuPortuguese:
                return "PT-BR";
            case GameText.MenuEnglish:
                return "EN";

            case GameText.TutorialStep1Title:
                return i_english ? "Initializing..." : "Iniciando...";
            case GameText.TutorialStep1Body:
                return i_english
                    ? "You are on the Nexus's Edge. The system won't let you out. You must fight your way out. \n \n Use WASD to move. \n LEFT STICK on controller. \n \n This enemy is your training dummy. Attack him with LEFT MOUSE or X on your controller. "
                    : "Você está no Nexus's Edge. O sistema não vai te deixar passar. Você irá precisar lutar para sair daqui. \n \n Use WASD para se mover. \n ALAVANCA DIREITA no controle. \n \n Esse inimigo é o seu boneco de treino. Ataque-o com o BOTÃO ESQUERDO ou X no seu controle.";
            case GameText.TutorialStep1Hint:
                return i_english ? "Hit the enemy to continue." : "Acerte o inimigo para avançar.";

            case GameText.TutorialStep2Title:
                return i_english ? "Connection Established" : "Conexão Estabelecida";
            case GameText.TutorialStep2Body:
                return i_english
                    ? "Getting hit depletes your health. \n \n Your health is show by the color of your LEDs. The same apply for your enemies. \n \n Avoid taking damage by evading with SPACEBAR \n Dodge with A on the controller. \n \n Be careful. Some enemies can dodge as well."
                    : "Ser atingido diminui a sua vida. \n \n Você pode ver o quanto de vida você tem pela cor de seus LEDs. O mesmo funciona para os inimigos. \n \n Evite dano usando a esquiva com a BARRA DE ESPAÇO \n Esquive com A no controle. \n \n Tome cuidado. Alguns inimigos podem desviar também.";
            case GameText.TutorialStep2Hint:
                return i_english ? "Dodge an attack to continue." : "Esquive de um ataque para avançar.";

            case GameText.TutorialStep3Title:
                return i_english ? "Initiating Firewall.exe" : "Iniciando Firewall.exe";
            case GameText.TutorialStep3Body:
                return i_english
                    ? "Hold RIGHT MOUSE BUTTON to block. \n RB on controller \n \n Blocking at the exact moment parries the attack. \n \n Parrying starts your boosted life and posture regeneration."
                    : "segure o BOTÃO DIREITO DO MOUSE para bloquear. \n RB no controle. \n \n Bloquear no momento exato apara o ataque. \n \n Aparos iniciam sua regeneração de vida e postura amplificada.";
            case GameText.TutorialStep3Hint:
                return i_english ? "Land a parry to continue." : "Acerte um aparo para avançar.";

            case GameText.TutorialStep4Title:
                return i_english ? "Installing DAEMONS" : "Instalando DAEMONS";
            case GameText.TutorialStep4Body:
                return i_english
                    ? "Some enemies can block your attacks with their shields. \n \n Blocking requires posture. Heavy attacks deals extra damage to posture. Using a heavy attack after a parry heavily increases its speed. \n \n Use heavy attacks with E or MIDDLE MOUSE BUTTON. \n Use Y on controller. \n \n Beware! You can also have your posture broken."
                    : "Alguns inimigos podem bloquear seus ataques com seus escudos. \n \n Bloquear requer postura. Ataques pesados dão dano extra na postura. Usar um ataque pesado após um aparo amplifica bastante sua velocidade. \n \n Use ataques pesados com E ou BOTÃO DO MEIO DO MOUSE. \n Use Y no controle. \n \n Cuidado! Você também pode ter sua postura quebrada.";
            case GameText.TutorialStep4Hint:
                return i_english ? "Hit the enemy with a heavy attack to continue." : "Acerte o inimigo com um ataque forte para avançar.";

            case GameText.TutorialStep5Title:
                return i_english ? "SYSTEM ERROR!!!" : "ERRO NO SISTEMA!!!";
            case GameText.TutorialStep5Body:
                return i_english
                    ? "The agents are coming. Attack them and avoid getting hit to increase you Synchronization Flow. \n \n With full Flow, a random agent will unleash the BreakFlow, a very powerfull attack. You must parry it to move forward.\n \n Do this enough times, and maybe you will escape. \n \n Good luck. I see you on the other side \n - 3r1k"
                    : "Os agentes estão vindo. Ataque os e evite receber golpes para aumentar o seu Fluxo de Sincronização. \n \n Quando seu Fluxo estiver no máximo, um agente irá realizar o BreakFlow, um ataque muito poderoso. Você deve apará-lo para prosseguir.\n \n Repita isso o bastante e talvez você consiga escapar. \n \n Boa sorte. Eu te vejo do outro lado. \n - 3r1k";
            case GameText.TutorialStep5Hint:
                return i_english ? "Parry the BreakFlow." : "Apare o BreakFlow.";

            case GameText.TutorialFinalTitle:
                return "---";
            case GameText.TutorialFinalBody:
                return i_english
                    ? "---."
                    : "---.";
            case GameText.TutorialFinalHint:
                return i_english ? "---" : "---";

            case GameText.TutorialBreakFlowTitle:
                return "---";
            case GameText.TutorialBreakFlowBody:
                return i_english
                    ? "---"
                    : "---.";
            case GameText.TutorialBreakFlowHint:
                return i_english ? "---" : "---";

            case GameText.TutorialSpecialTitle:
                return i_english ? "---" : "---";
            case GameText.TutorialSpecialBody:
                return i_english
                    ? "---"
                    : "---";
            case GameText.TutorialSpecialHint:
                return i_english ? "---" : "---";

            case GameText.TutorialFailEvadeInsteadParry:
                return i_english
                    ? "ERROR 208: Attack must be PARRIED and not dodge."
                    : "ERRO 208: Ataque deve ser APARADO e não esquivado.";
            case GameText.TutorialFailParryInsteadDodge:
                return i_english
                    ? "ERROR 204: Attack requires a DODGE instead of a parry."
                    : "ERRO 204: Ataque exige ESQUIVA ao invés de aparo.";
            case GameText.TutorialFailBlocked:
                return i_english
                    ? "ERROR 112: Use the action requested in the panel instead of a block."
                    : "ERRO 112: Use a ação pedida no painel para avançar ao invés de um bloqueio.";
            case GameText.TutorialFailHitDodge:
                return i_english
                    ? "ERROR 104: You were hit. Try DODGING again."
                    : "ERRO 104: Você foi atingido. Tente ESQUIVAR novamente.";
            case GameText.TutorialFailHitParry:
                return i_english
                    ? "ERROR 107: You were hit. Try the parry again right before the attack."
                    : "ERRO 107: Você foi atingido. Tente o aparo novamente logo antes do ataque.";
            case GameText.TutorialFailUseHeavy:
                return i_english
                    ? "ERROR 329: Use a HEAVY ATTACK instead of a light attack."
                    : "ERRO 329: Use um ATAQUE PESADO ao invés de um ataque leve.";
            case GameText.TutorialFailBreakFlow:
                return i_english
                    ? "ERROR 001: You failed to parry the BreakFlow..."
                    : "ERRO 001: Você falhou em aparar o BreakFlow...";

            case GameText.TutorialCompleteTitle:
                return "S3CUR1TY BR34CH3D";
            case GameText.TutorialCompleteBody:
                return "01000101 01110010 01101001 01100011 00100000 11000011 10101001 00100000 01100100 01100101 01101101 01100001 01101001 01110011 00100001";
            case GameText.TutorialCompleteHint:
                return "ERROR ERROR ERROR ERROR";

            case GameText.VictoryTitle:
                return i_english ? "VICTORY" : "VITÓRIA";
            case GameText.VictoryBody:
                return i_english ? "You escaped the Nexus." : "Você escapou do Nexus.";
            case GameText.DefeatTitle:
                return i_english ? "DEFEAT" : "DERROTA";
            case GameText.DefeatBody:
                return i_english ? "You fell in the arena." : "Você caiu na arena.";
            case GameText.PauseTitle:
                return i_english ? "PAUSED" : "PAUSADO";
            case GameText.PauseBody:
                return i_english ? "Press ESC or P to continue." : "Pressione ESC ou P para continuar.";
            case GameText.Resume:
                return i_english ? "RESUME" : "CONTINUAR";
            case GameText.Restart:
                return i_english ? "RESTART" : "REINICIAR";
            case GameText.Quit:
                return i_english ? "QUIT" : "SAIR";

            default:
                return string.Empty;
        }
    }

    static public void NotifyLanguageChanged()
    {
        LanguageChanged?.Invoke();
    }
}
