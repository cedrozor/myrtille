function createPopupI18n() {
    var translations = {
        en: {
            close: 'Close',
            send: 'Send',
            copy: 'Copy',
            upload: 'Upload',
            download: 'Download',
            create: 'Create',
            save: 'Save',
            delete: 'Delete',
            connect: 'Connect',
            changePassword: 'Change Password',
            createSingleUseSessionUrl: 'Create Single Use Session URL',
            pasteClipboardDesc: 'Type or paste some text then click send',
            clipboardSyncSuccess: 'Clipboard synchronized successfully',
            clipboardSyncIe: 'IE detected, press Ctrl+C (Cmd-C on Mac) to synchronize the clipboard',
            clipboardSyncFailed: 'Clipboard was not synchronized (execCommand failed or copy was denied)',
            clipboardSendFailed: 'Failed to send clipboard: ',
            copyClipboardDesc: 'Copy or press Ctrl+C (Cmd-C on Mac)',
            clipboardCopyFailed: 'Failed to copy clipboard: ',
            virtualKeyboardDesc: 'Type or paste some text then click send',
            virtualKeyboardAlt: 'Alternatively, use the Windows on-screen keyboard (%SystemRoot%\\System32\\osk.exe) within the session',
            fileStorageDesc: 'Files in the "My Documents" folder',
            uploadFile: 'Upload file:',
            downloadFile: 'Download file:',
            fileUploadSuccess: 'File uploaded successfully',
            shareSessionTitle: 'Share Session',
            grantControl: 'Grant control',
            grantControlTitle: 'Allow the guest to interact with the remote session',
            sessionUrl: 'Session URL',
            sessionUrlHint: 'Copy the URL as needed; it can only be used once',
            hostConfiguration: 'Host Configuration',
            hostName: 'Host name',
            hostNameTitle: 'Host name',
            hostAddress: 'Host address (:port) (optional; uses host name if omitted)',
            hostAddressTitle: 'Host address',
            vmGuidOptional: 'VM GUID (optional, if the guest is a Hyper-V VM)',
            vmGuidDirectTitle: 'VM GUID for direct connection',
            vmEnhancedMode: 'VM Enhanced Mode (if supported by the VM)',
            vmEnhancedModePopupTitle: 'Faster display and clipboard/printer redirection if supported by the guest VM',
            groupsAccess: 'Allowed domain groups or users (comma separated)',
            groupsAccessTitle: 'Access restrictions',
            rdpSecurityProtocol: 'RDP Security Protocol',
            rdpSecurityTitle: 'NLA = safest; RDP = backward compatibility; AUTO for Hyper-V VM or if unsure',
            startRemoteApp: 'Start Remote App Program',
            startRemoteAppTitle: 'Remote program to run on session start; unavailable for Hyper-V VM',
            promptCredentials: 'Prompt for Credentials',
            promptCredentialsTitle: 'Prompt for credentials or use the signed-in user',
            createHostSessionFor: 'Create Host Session for ',
            domainOptional: 'Domain (optional)',
            userDomainTitle: 'User domain',
            username: 'Username',
            userNameTitle: 'User name',
            password: 'Password',
            userPasswordTitle: 'User password',
            loginCredentials: 'Login Credentials',
            userName: 'User name',
            oldPassword: 'Old Password',
            oldPasswordTitle: 'Old password',
            newPassword: 'New Password',
            newPasswordTitle: 'New password',
            confirmPassword: 'Confirm Password',
            confirmPasswordTitle: 'Confirm password',
            changePasswordMessage: 'Your password has expired and must be changed',
            keyboardDefault: 'Default',
            keyboardChinese: 'Chinese',
            keyboardEnglish: 'English',
            keyboardWarning: 'Ensure the keyboard layout is en-US (QWERTY) in the remote session'
        },
        'zh-CN': {
            close: '关闭',
            send: '发送',
            copy: '复制',
            upload: '上传',
            download: '下载',
            create: '创建',
            save: '保存',
            delete: '删除',
            connect: '连接',
            changePassword: '修改密码',
            createSingleUseSessionUrl: '创建单次可用会话链接',
            pasteClipboardDesc: '输入或粘贴文本后点击发送',
            clipboardSyncSuccess: '剪贴板同步成功',
            clipboardSyncIe: '检测到 IE，如需同步剪贴板请按 Ctrl+C（Mac 上为 Cmd-C）',
            clipboardSyncFailed: '剪贴板未同步成功（execCommand 调用失败或复制被拒绝）',
            clipboardSendFailed: '发送剪贴板失败：',
            copyClipboardDesc: '复制或按 Ctrl+C（Mac 上为 Cmd-C）',
            clipboardCopyFailed: '复制剪贴板失败：',
            virtualKeyboardDesc: '输入或粘贴文本后点击发送',
            virtualKeyboardAlt: '也可以在会话中使用 Windows 屏幕键盘（%SystemRoot%\\System32\\osk.exe）',
            fileStorageDesc: '位于“我的文档”文件夹中的文件',
            uploadFile: '上传文件：',
            downloadFile: '下载文件：',
            fileUploadSuccess: '文件上传成功',
            shareSessionTitle: '共享会话',
            grantControl: '允许控制',
            grantControlTitle: '允许访客与远程会话交互',
            sessionUrl: '会话链接',
            sessionUrlHint: '复制该链接后按需使用；它只能使用一次',
            hostConfiguration: '主机配置',
            hostName: '主机名称',
            hostNameTitle: '主机名称',
            hostAddress: '主机地址（:端口）（可选；未填写时使用主机名）',
            hostAddressTitle: '主机地址',
            vmGuidOptional: 'VM GUID（可选，如果来宾是 Hyper-V 虚拟机）',
            vmGuidDirectTitle: '用于直连的 VM GUID',
            vmEnhancedMode: 'VM 增强模式（如果虚拟机支持）',
            vmEnhancedModePopupTitle: '如果来宾虚拟机支持，将提供更快显示和剪贴板/打印机重定向',
            groupsAccess: '允许的域组或用户（逗号分隔）',
            groupsAccessTitle: '访问限制',
            rdpSecurityProtocol: 'RDP 安全协议',
            rdpSecurityTitle: 'NLA = 最安全；RDP = 向后兼容；Hyper-V VM 或不确定时可选 AUTO',
            startRemoteApp: '启动远程应用程序',
            startRemoteAppTitle: '会话启动时运行的远程程序；Hyper-V VM 不可用',
            promptCredentials: '连接时输入凭据',
            promptCredentialsTitle: '提示输入凭据，否则使用当前登录用户',
            createHostSessionFor: '为以下主机创建会话：',
            domainOptional: '域（可选）',
            userDomainTitle: '用户域',
            username: '用户名',
            userNameTitle: '用户名',
            password: '密码',
            userPasswordTitle: '用户密码',
            loginCredentials: '登录凭据',
            userName: '用户名',
            oldPassword: '旧密码',
            oldPasswordTitle: '旧密码',
            newPassword: '新密码',
            newPasswordTitle: '新密码',
            confirmPassword: '确认密码',
            confirmPasswordTitle: '确认密码',
            changePasswordMessage: '你的密码已过期，必须修改',
            keyboardDefault: '默认',
            keyboardChinese: '中文',
            keyboardEnglish: '英文',
            keyboardWarning: '请确保远程会话中的键盘布局为 en-US（QWERTY）'
        }
    };

    function resolveLanguage() {
        try {
            var queryMatch = window.location.search.match(/[?&]lang=([^&]+)/i);
            if (queryMatch && translations[decodeURIComponent(queryMatch[1])]) {
                return decodeURIComponent(queryMatch[1]);
            }
        }
        catch (exc) {}

        try {
            if (window.parent && window.parent !== window && window.parent.appLanguage && translations[window.parent.appLanguage]) {
                return window.parent.appLanguage;
            }
        }
        catch (exc2) {}

        try {
            var storedLanguage = localStorage.getItem('myrtilleLanguage');
            if (storedLanguage && translations[storedLanguage]) {
                return storedLanguage;
            }
        }
        catch (exc3) {}

        return 'en';
    }

    var language = resolveLanguage();

    function t(key) {
        var locale = translations[language] || translations.en;
        return locale[key] || translations.en[key] || key;
    }

    function apply() {
        document.documentElement.lang = language;

        var textNodes = document.querySelectorAll('[data-i18n]');
        for (var i = 0; i < textNodes.length; i++) {
            textNodes[i].textContent = t(textNodes[i].getAttribute('data-i18n'));
        }

        var htmlNodes = document.querySelectorAll('[data-i18n-html]');
        for (var j = 0; j < htmlNodes.length; j++) {
            htmlNodes[j].innerHTML = t(htmlNodes[j].getAttribute('data-i18n-html'));
        }

        var titleNodes = document.querySelectorAll('[data-i18n-title]');
        for (var k = 0; k < titleNodes.length; k++) {
            titleNodes[k].setAttribute('title', t(titleNodes[k].getAttribute('data-i18n-title')));
        }

        var valueNodes = document.querySelectorAll('[data-i18n-value]');
        for (var m = 0; m < valueNodes.length; m++) {
            valueNodes[m].value = t(valueNodes[m].getAttribute('data-i18n-value'));
        }
    }

    return {
        language: language,
        t: t,
        apply: apply
    };
}

window.popupI18n = createPopupI18n();
