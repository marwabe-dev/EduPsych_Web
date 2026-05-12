
    function toggleSmartChat() {
        const panel = document.getElementById('ai-glass-panel');
    panel.classList.toggle('hidden');
    }

    function handleAiKey(e) {
        if (e.key === 'Enter') processAiRequest();
    }

    async function processAiRequest() {
        const input = document.getElementById('ai-user-query');
    const query = input.value.trim();
    if (!query) return;

    addBubble(query, 'user');
    input.value = '';

    // مؤشر ذكي
    const loaderId = "loading-" + Date.now();
    addBubble("جاري قراءة تساؤلك...", 'ai', loaderId);

    try {
            const response = await fetch('/api/ChatAi/ask', {
        method: 'POST',
    headers: {'Content-Type': 'application/json' },
    body: JSON.stringify({prompt: query })
            });

    const data = await response.json();
    document.getElementById(loaderId).closest('.ai-bubble-wrap').remove();

    if (data.response) {
        addBubble(data.response, 'ai');
            }
        } catch (error) {
        document.getElementById(loaderId).innerText = "تعذر الاتصال بالمحرك.";
        }
    }

    function addBubble(text, sender, id = null) {
        const body = document.getElementById('ai-chat-body');
    const wrap = document.createElement('div');
    wrap.className = `ai-bubble-wrap ${sender === 'user' ? 'ai-end' : 'ai-start'}`;

    const bubble = document.createElement('div');
    bubble.className = 'ai-bubble';
    if (id) bubble.id = id;
    bubble.innerText = text;

    wrap.appendChild(bubble);
    body.appendChild(wrap);
    body.scrollTop = body.scrollHeight;
    }
