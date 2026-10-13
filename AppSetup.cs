using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

internal static class AppSetup
{
    private const string AgreementVersion = "1";
    private sealed class TranslationTable : Dictionary<string, string>
    {
        public new void Add(string key, string value)
        {
            this[key] = value;
        }
    }

    private static readonly Dictionary<string, string> Portuguese = new TranslationTable
    {
        { "Back", "Voltar" }, { "Next", "Avançar" }, { "Cancel", "Cancelar" }, { "Close", "Fechar" },
        { "Repair", "Reparar" }, { "Scan", "Analisar" }, { "Welcome to Splint", "Bem vindo ao Splint" },
        { "Scan type", "Tipo de análise" }, { "Scan in progress", "Análise em andamento" }, { "Scan results", "Resultados da análise" },
        { "Repair what an infection leaves behind.", "Encontre sinais que uma infecção pode deixar." },
        { "Splint inspects this PC for spyware and persistence indicators. It is not real time protection.", "O Splint procura sinais de spyware e persistência neste computador. Ele não oferece proteção em tempo real." },
        { "Splint inspects this PC for spyware and persistence indicators. Keep your antivirus running.", "O Splint procura sinais de spyware e persistência neste computador. Mantenha seu antivírus ativo." },
        { "Portable edition  •  No driver  •  ", "Edição portátil  •  Sem driver  •  " },
        { "Choose how thoroughly Splint should look", "Escolha o tipo de análise" },
        { "Quick scan", "Análise rápida" }, { "Full scan", "Análise completa" }, { "Custom scan", "Análise personalizada" },
        { "Your user profile, startup folders and Run key targets.", "Seu perfil de usuário, pastas de inicialização e alvos das chaves Run." },
        { "Everything in your user profile.", "Todo o perfil do usuário." },
        { "Everything in your user profile. This can take a long time.", "Todo o perfil do usuário. A análise pode demorar." },
        { "Only the folder you choose.", "Somente a pasta escolhida." },
        { "Please wait while Splint checks this PC", "Aguarde enquanto o Splint analisa este computador" },
        { "Review what Splint found", "Revise os resultados encontrados" },
        { "Choose a folder to scan", "Escolha uma pasta para analisar" }, { "Selected folder: ", "Pasta selecionada: " },
        { "No accessible scan locations were found.", "Nenhum local acessível foi encontrado para análise." },
        { "Preparing scan...", "Preparando a análise..." }, { "Checking: ", "Verificando: " },
        { " item(s) flagged so far", " item(ns) sinalizado(s) até agora" }, { "The scan stopped: ", "A análise foi interrompida: " },
        { "Threat", "Ameaça" }, { "Heuristic review", "Revisão heurística" }, { "Hash database match", "Correspondência na base de hashes" },
        { "Found", "Identificado" }, { "Review only", "Somente para revisão" }, { "Scan canceled. Partial results: ", "Análise cancelada. Resultados parciais: " },
        { "nothing was flagged in ", "nenhum item foi sinalizado entre " }, { " items.", " itens." },
        { " item(s) can be repaired; ", " item(ns) pode(m) ser colocado(s) em quarentena; " },
        { " item(s) are review only.", " item(ns) requer(em) somente revisão." },
        { " item(s) are review only (", " item(ns) requer(em) somente revisão (" },
        { " hash match(es)).", " correspondência(s) de hash)." },
        { "Move the selected ", "Mover " }, { " item(s) into quarantine?", " item(ns) selecionado(s) para a quarentena?" },
        { "Confirm repair", "Confirmar quarentena" }, { "Quarantined", "Em quarentena" },
        { "Could not move", "Não foi possível mover" },
        { " item(s) moved to quarantine. The scan report is saved separately.", " item(ns) movido(s) para a quarentena. O relatório foi salvo separadamente." },
        { "Repair what an infection leaves behind", "Encontre sinais que uma infecção pode deixar" },
        { "Choose how thoroughly Splint should look", "Escolha o tipo de análise" },
        { "Please wait while Splint checks this PC", "Aguarde enquanto o Splint analisa este computador" },
        { "Review what Splint found", "Revise os resultados encontrados" },
        { "Smart Heuristics", "Análise heurística" }, { "Analyzes entropy and APIs.", "Analisa entropia e indicadores de API." },
        { "Analyzes file indicators and publisher information.", "Analisa sinais dos arquivos e informações do editor." },
        { "Spyware detection", "Detecção de spyware" }, { "Identifies keyloggers, screen capturers, and credential stealers hidden in user folders.", "Procura registradores de teclas, captura de tela e roubo de credenciais em pastas do usuário." },
        { "File review", "Revisão de arquivos" }, { "Checks known signatures and publisher details.", "Verifica assinaturas conhecidas e dados do editor." },
        { "Threats", "Ameaças" }, { "Persistence and malware", "Persistência e malware" },
        { "Spyware", "Spyware" }, { "Surveillance and stealers", "Vigilância e ladrões de dados" },
        { "Repair what an infection leaves behind.", "Encontre sinais que uma infecção pode deixar." },
        { "files checked", "arquivos analisados" }, { "files checked", "arquivos analisados" },
        { "Checking: ", "Verificando: " }, { " item(s) flagged so far", " item(ns) sinalizado(s) até agora" },
        { "Repair", "Quarentena" }, { "Type", "Tipo" }, { "Item", "Item" }, { "Details", "Detalhes" }, { "Status", "Status" },
        { "Enter your product key:", "Digite sua chave de produto:" }, { "Activate", "Ativar" },
        { "Invalid product key.", "A chave de produto é inválida." },
        { "SPLINT could not start: ", "Não foi possível iniciar o SPLINT: " }
    };

    private static readonly Dictionary<string, string> Russian = new TranslationTable
    {
        { "Back", "Назад" }, { "Next", "Далее" }, { "Cancel", "Отмена" }, { "Close", "Закрыть" },
        { "Repair", "Карантин" }, { "Scan", "Сканировать" }, { "Welcome to Splint", "Добро пожаловать в Splint" },
        { "Scan type", "Тип проверки" }, { "Scan in progress", "Проверка выполняется" }, { "Scan results", "Результаты проверки" },
        { "Repair what an infection leaves behind.", "Найдите признаки, оставленные заражением." },
        { "Splint inspects this PC for spyware and persistence indicators. It is not real time protection.", "Splint ищет признаки шпионских программ и закрепления в системе. Это не защита в реальном времени." },
        { "Splint inspects this PC for spyware and persistence indicators. Keep your antivirus running.", "Splint ищет признаки шпионских программ и закрепления в системе. Не отключайте антивирус." },
        { "Portable edition  •  No driver  •  ", "Портативная версия  •  Без драйверов  •  " },
        { "Choose how thoroughly Splint should look", "Выберите тип проверки" },
        { "Quick scan", "Быстрая проверка" }, { "Full scan", "Полная проверка" }, { "Custom scan", "Выборочная проверка" },
        { "Your user profile, startup folders and Run key targets.", "Профиль пользователя, папки автозагрузки и цели ключей Run." },
        { "Everything in your user profile.", "Весь профиль пользователя." },
        { "Everything in your user profile. This can take a long time.", "Весь профиль пользователя. Проверка может занять много времени." },
        { "Only the folder you choose.", "Только выбранная папка." },
        { "Please wait while Splint checks this PC", "Подождите, Splint проверяет компьютер" },
        { "Review what Splint found", "Проверьте найденные элементы" },
        { "Choose a folder to scan", "Выберите папку для проверки" }, { "Selected folder: ", "Выбрана папка: " },
        { "No accessible scan locations were found.", "Не найдено доступных мест для проверки." },
        { "Preparing scan...", "Подготовка проверки..." }, { "Checking: ", "Проверяется: " },
        { " item(s) flagged so far", " элемент(ов) отмечено" }, { "The scan stopped: ", "Проверка остановлена: " },
        { "Threat", "Угроза" }, { "Heuristic review", "Эвристическая проверка" }, { "Hash database match", "Совпадение с базой хешей" },
        { "Found", "Обнаружено" }, { "Review only", "Только проверка" }, { "Scan canceled. Partial results: ", "Проверка отменена. Частичные результаты: " },
        { "nothing was flagged in ", "не отмечено элементов: " }, { " items.", " ." },
        { " item(s) can be repaired; ", " элемент(ов) можно поместить в карантин; " },
        { " item(s) are review only.", " элемент(ов) требуют проверки." },
        { " item(s) are review only (", " элемент(ов) требуют проверки (" },
        { " hash match(es)).", " совпадений хеша)." },
        { "Move the selected ", "Переместить выбранные " }, { " item(s) into quarantine?", " элемент(ы) в карантин?" },
        { "Confirm repair", "Подтвердить карантин" }, { "Quarantined", "В карантине" },
        { "Could not move", "Не удалось переместить" },
        { " item(s) moved to quarantine. The scan report is saved separately.", " элемент(ов) перемещено в карантин. Отчёт сохранён отдельно." },
        { "Repair what an infection leaves behind", "Найдите признаки, оставленные заражением" },
        { "Choose how thoroughly Splint should look", "Выберите тип проверки" },
        { "Please wait while Splint checks this PC", "Подождите, Splint проверяет компьютер" },
        { "Review what Splint found", "Проверьте найденные элементы" },
        { "Smart Heuristics", "Эвристический анализ" }, { "Analyzes entropy and APIs.", "Анализирует энтропию и признаки API." },
        { "Analyzes file indicators and publisher information.", "Анализирует признаки файлов и сведения об издателе." },
        { "Spyware detection", "Поиск шпионских программ" }, { "Identifies keyloggers, screen capturers, and credential stealers hidden in user folders.", "Ищет программы записи клавиш, снимков экрана и кражи учётных данных в папках пользователя." },
        { "File review", "Проверка файлов" }, { "Checks known signatures and publisher details.", "Проверяет известные сигнатуры и сведения об издателе." },
        { "Threats", "Угрозы" }, { "Persistence and malware", "Закрепление и вредоносные программы" },
        { "Spyware", "Шпионские программы" }, { "Surveillance and stealers", "Наблюдение и кража данных" },
        { "Repair what an infection leaves behind.", "Найдите признаки, оставленные заражением." },
        { "files checked", "файлов проверено" },
        { "Checking: ", "Проверяется: " }, { " item(s) flagged so far", " элемент(ов) отмечено" },
        { "Type", "Тип" }, { "Item", "Объект" }, { "Details", "Сведения" }, { "Status", "Состояние" },
        { "Enter your product key:", "Введите ключ продукта:" }, { "Activate", "Активировать" },
        { "Invalid product key.", "Неверный ключ продукта." },
        { "SPLINT could not start: ", "Не удалось запустить SPLINT: " }
    };

    internal static string Language { get; private set; } = "en";

    internal static string T(string text)
    {
        Dictionary<string, string> values = Language == "pt" ? Portuguese : Language == "ru" ? Russian : null;
        string translated;
        return values != null && values.TryGetValue(text, out translated) ? translated : text;
    }

    internal static bool Initialize(IWin32Window owner, string dataDirectory, string tier)
    {
        Directory.CreateDirectory(dataDirectory);
        string settingsPath = Path.Combine(dataDirectory, "settings.ini");
        var settings = ReadSettings(settingsPath);
        bool accepted = settings.ContainsKey("EulaVersion") && settings["EulaVersion"] == AgreementVersion;
        if (!accepted && !ShowAgreement(owner, tier))
            return false;

        string selectedLanguage;
        if (!settings.TryGetValue("Language", out selectedLanguage) ||
            (selectedLanguage != "en" && selectedLanguage != "pt" && selectedLanguage != "ru"))
        {
            selectedLanguage = SelectLanguage(owner);
            if (selectedLanguage == null)
                return false;
        }

        settings["EulaVersion"] = AgreementVersion;
        settings["Language"] = selectedLanguage;
        WriteSettings(settingsPath, settings);
        Language = selectedLanguage;
        return true;
    }

    private static Dictionary<string, string> ReadSettings(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(path))
            return values;
        foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
        {
            int separator = line.IndexOf('=');
            if (separator > 0)
                values[line.Substring(0, separator)] = line.Substring(separator + 1);
        }
        return values;
    }

    private static void WriteSettings(string path, Dictionary<string, string> values)
    {
        string temporary = path + ".tmp";
        var content = new StringBuilder();
        foreach (var item in values)
            content.Append(item.Key).Append('=').Append(item.Value).AppendLine();
        File.WriteAllText(temporary, content.ToString(), new UTF8Encoding(false));
        if (File.Exists(path))
            File.Replace(temporary, path, null);
        else
            File.Move(temporary, path);
    }

    private static bool ShowAgreement(IWin32Window owner, string tier)
    {
        string featureText = tier == "PRO"
            ? "PRO includes additional spyware indicators and review information."
            : "FREE provides a basic scan of the selected locations.";
        using (var dialog = new Form())
        using (var document = new TextBox())
        using (var acknowledgement = new CheckBox())
        using (var accept = new Button())
        using (var decline = new Button())
        {
            dialog.Text = "SPLINT " + tier + " End User License Agreement";
            dialog.ClientSize = new System.Drawing.Size(700, 520);
            dialog.MinimumSize = new System.Drawing.Size(580, 420);
            dialog.StartPosition = FormStartPosition.CenterScreen;
            dialog.FormBorderStyle = FormBorderStyle.Sizable;
            dialog.MinimizeBox = false;
            dialog.MaximizeBox = true;
            dialog.Font = new System.Drawing.Font("Segoe UI", 9F);
            var title = new Label { Text = "END USER LICENSE AGREEMENT", Font = new System.Drawing.Font("Segoe UI Semibold", 14F), Location = new System.Drawing.Point(20, 16), Size = new System.Drawing.Size(640, 30) };
            document.Location = new System.Drawing.Point(20, 56);
            document.Size = new System.Drawing.Size(660, 350);
            document.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            document.Multiline = true;
            document.ReadOnly = true;
            document.ScrollBars = ScrollBars.Vertical;
            document.WordWrap = true;
            document.Text = "SPLINT SOFTWARE LICENSE AND USER NOTICE\r\n\r\n" +
                "1. Permission and scope. You may install and use this copy of SPLINT for lawful security review on a computer or files that you own or are authorized to administer. The software is a local, on demand second opinion scanner. It is not real time antivirus protection and must not replace your primary security software.\r\n\r\n" +
                "2. What the software does. SPLINT enumerates files within the scan locations you select and checks them using its included hash data, static rules and heuristic indicators. Depending on the edition, it may also inspect file publisher signatures and selected startup or registry locations. " + featureText + "\r\n\r\n" +
                "3. Findings and limitations. A match or heuristic result is an indicator for human review, not proof that a file is malicious. False positives and missed detections are possible. The software does not guarantee that a computer is clean or secure. Keep backups and use your primary antivirus and other appropriate safeguards.\r\n\r\n" +
                "4. Actions and local data. Scan reports, settings, activation data and quarantine metadata are written to the SPLINT data folder on this computer. If you choose quarantine, SPLINT moves the selected file and records its original path and hash. Registry changes, where offered, require a separate explicit action and a backup is created first. These actions can affect software or system behavior. Review each item and make sure you have authority before proceeding.\r\n\r\n" +
                "5. Privacy. File inspection and report creation are performed locally. SPLINT does not intentionally upload scanned files or reports. Do not submit private files to any third party unless you have reviewed that party's terms and have authority to share them.\r\n\r\n" +
                "6. Warranty and liability. The software is provided as is, without warranties to the extent permitted by applicable law. You are responsible for deciding whether to scan, quarantine or modify an item and for maintaining recoverable backups. Nothing in this agreement limits rights that cannot be limited under applicable law.\r\n\r\n" +
                "By selecting I agree, you confirm that you have read this notice and will use SPLINT only on systems and data you are authorized to inspect.";
            acknowledgement.Text = "I have read and agree to this EULA.";
            acknowledgement.Location = new System.Drawing.Point(20, 418);
            acknowledgement.Size = new System.Drawing.Size(420, 26);
            acknowledgement.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            accept.Text = "I Agree";
            accept.Enabled = false;
            accept.DialogResult = DialogResult.OK;
            accept.Location = new System.Drawing.Point(484, 466);
            accept.Size = new System.Drawing.Size(92, 32);
            accept.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            decline.Text = "Decline";
            decline.DialogResult = DialogResult.Cancel;
            decline.Location = new System.Drawing.Point(584, 466);
            decline.Size = new System.Drawing.Size(92, 32);
            decline.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            acknowledgement.CheckedChanged += (sender, args) => accept.Enabled = acknowledgement.Checked;
            dialog.Controls.AddRange(new Control[] { title, document, acknowledgement, accept, decline });
            dialog.AcceptButton = accept;
            dialog.CancelButton = decline;
            return dialog.ShowDialog(owner) == DialogResult.OK && acknowledgement.Checked;
        }
    }

    private static string SelectLanguage(IWin32Window owner)
    {
        using (var dialog = new Form())
        using (var language = new ComboBox())
        using (var save = new Button())
        using (var cancel = new Button())
        {
            dialog.Text = "SPLINT language";
            dialog.ClientSize = new System.Drawing.Size(440, 180);
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.StartPosition = FormStartPosition.CenterScreen;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            var label = new Label { Text = "Choose the language for SPLINT:", Location = new System.Drawing.Point(20, 24), Size = new System.Drawing.Size(390, 28) };
            language.DropDownStyle = ComboBoxStyle.DropDownList;
            language.Items.AddRange(new object[] { "English", "Português", "Русский" });
            language.SelectedIndex = 0;
            language.Location = new System.Drawing.Point(20, 58);
            language.Size = new System.Drawing.Size(390, 28);
            save.Text = "Continue";
            save.DialogResult = DialogResult.OK;
            save.Location = new System.Drawing.Point(224, 120);
            save.Size = new System.Drawing.Size(88, 32);
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Location = new System.Drawing.Point(322, 120);
            cancel.Size = new System.Drawing.Size(88, 32);
            dialog.Controls.AddRange(new Control[] { label, language, save, cancel });
            dialog.AcceptButton = save;
            dialog.CancelButton = cancel;
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return null;
            return language.SelectedIndex == 1 ? "pt" : language.SelectedIndex == 2 ? "ru" : "en";
        }
    }
}
