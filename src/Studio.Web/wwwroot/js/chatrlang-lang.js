window.chatrlangLang = {
    register: function () {
        if (window._chatrlangRegistered) return;
        window._chatrlangRegistered = true;

        require(['vs/editor/editor.main'], function () {
            monaco.languages.register({ id: 'chatrlang' });

            monaco.languages.setMonarchTokensProvider('chatrlang', {
                keywords: ['create', 'table', 'mut', 'string'],
                tokenizer: {
                    root: [
                        [/"""[\s\S]*?"""/, 'string'],
                        [/'''[\s\S]*?'''/, 'string'],
                        [/[a-zA-Z_][a-zA-Z0-9_]*/, {
                            cases: {
                                '@keywords': 'keyword',
                                '@default': 'identifier'
                            }
                        }],
                        [/[(),;:]/, 'delimiter'],
                        [/\s+/, 'white']
                    ]
                }
            });

            monaco.languages.setLanguageConfiguration('chatrlang', {
                brackets: [['(', ')']],
                autoClosingPairs: [{ open: '(', close: ')' }],
                comments: {}
            });
        });
    }
};
