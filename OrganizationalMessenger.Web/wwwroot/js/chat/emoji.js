// ============================================
// Emoji Picker
// ============================================

import { emojiPickerVisible, setEmojiPickerVisible } from './variables.js';

export function toggleEmojiPicker() {
    let container = document.getElementById('emojiPickerContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'emojiPickerContainer';
        document.body.appendChild(container);
    }

    if (!container.innerHTML) {
        container.innerHTML = createMatrixEmojiPickerHTML();
        setupEmojiPickerEvents();
    }

    setEmojiPickerVisible(!emojiPickerVisible);

    if (emojiPickerVisible) {
        container.style.display = 'block';

        const emojiBtn = document.getElementById('emojiBtn');
        const pickerEl = container.querySelector('.mx_ContextualMenu');

        if (emojiBtn && pickerEl) {
            setTimeout(() => {
                adjustEmojiPickerPosition();
            }, 50);
        }
    } else {
        container.style.display = 'none';
    }
}

function adjustEmojiPickerPosition() {
    const emojiBtn = document.getElementById('emojiBtn');
    const pickerEl = document.querySelector('.mx_ContextualMenu');

    if (!emojiBtn || !pickerEl) return;

    const btnRect = emojiBtn.getBoundingClientRect();
    const pickerRect = pickerEl.getBoundingClientRect();
    const pickerHeight = pickerRect.height;
    const pickerWidth = pickerRect.width;

    const spaceAbove = btnRect.top;
    const spaceBelow = window.innerHeight - btnRect.bottom;

    let top, left;

    if (spaceAbove > pickerHeight + 20) {
        top = btnRect.top - pickerHeight - 10;
    }
    else if (spaceBelow > pickerHeight + 20) {
        top = btnRect.bottom + 10;
    }
    else {
        top = Math.max(20, (window.innerHeight - pickerHeight) / 2);
    }

    left = btnRect.right - pickerWidth;

    left = Math.max(10, Math.min(left, window.innerWidth - pickerWidth - 10));
    top = Math.max(10, Math.min(top, window.innerHeight - pickerHeight - 10));

    pickerEl.style.top = `${top}px`;
    pickerEl.style.left = `${left}px`;

    console.log('📍 Adjusted Emoji Picker:', { top, left, pickerHeight });
}

function createMatrixEmojiPickerHTML() {
    return `
        <div class="mx_ContextualMenu mx_visible">
            <section class="mx_EmojiPicker">
                <nav class="mx_EmojiPicker_header">
                    <button class="mx_EmojiPicker_anchor active" data-category="recent" title="پرکاربرد">🕒</button>
                    <button class="mx_EmojiPicker_anchor" data-category="people" title="افراد">😀</button>
                    <button class="mx_EmojiPicker_anchor" data-category="nature" title="طبیعت">🐱</button>
                    <button class="mx_EmojiPicker_anchor" data-category="food" title="غذا">🍔</button>
                    <button class="mx_EmojiPicker_anchor" data-category="symbols" title="نمادها">❤️</button>
                </nav>
                <div class="mx_EmojiPicker_body">
                    <section class="mx_EmojiPicker_category active" data-category="recent">
                        <h2 class="mx_EmojiPicker_category_label">پرکاربرد</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getRecentEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                    <section class="mx_EmojiPicker_category" data-category="people">
                        <h2 class="mx_EmojiPicker_category_label">اف��اد</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getPeopleEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                    <section class="mx_EmojiPicker_category" data-category="nature">
                        <h2 class="mx_EmojiPicker_category_label">طبیعت</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getNatureEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                    <section class="mx_EmojiPicker_category" data-category="food">
                        <h2 class="mx_EmojiPicker_category_label">غذا</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getFoodEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                    <section class="mx_EmojiPicker_category" data-category="symbols">
                        <h2 class="mx_EmojiPicker_category_label">نمادها</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getSymbolsEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                </div>
                <section class="mx_EmojiPicker_footer">
                    <h2 class="mx_EmojiPicker_quick_header">واکنش سریع</h2>
                    <div class="mx_EmojiPicker_quick_list">
                        ${['👍', '👎', '😂', '❤️', '🎉', '😢', '🔥', '👀'].map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                    </div>
                </section>
            </section>
        </div>
    `;
}

function getRecentEmojis() {
    return ['😂', '❤️', '😍', '👍', '🔥', '🙏', '😊', '😘', '💯', '✨', '🎉', '👏', '💪', '🌹', '☺️', '😭', '🥰', '😁', '🤗', '💕', '🙌', '✅', '👌', '💖'];
}

function getPeopleEmojis() {
    return [
        '😀', '😃', '😄', '😁', '😆', '😅', '😂', '🤣', '😊', '😇', '🙂', '🙃', '😉', '😌', '😍', '🥰',
        '😘', '😗', '😙', '😚', '😋', '😛', '😝', '😜', '🤪', '🤨', '🧐', '🤓', '😎', '🤩', '🥳', '😏',
        '😒', '😞', '😔', '😟', '😕', '🙁', '☹️', '😣', '😖', '😫', '😩', '🥺', '😢', '😭', '😤', '😠',
        '😡', '🤬', '🤯', '😳', '🥵', '🥶', '😱', '😨', '😰', '😥', '😓', '🤗', '🤔', '🤭', '🤫', '🤥',
        '😶', '😐', '😑', '😬', '🙄', '😯', '😦', '😧', '😮', '😲', '🥱', '😴', '🤤', '😪', '😵', '🤐'
    ];
}

function getNatureEmojis() {
    return [
        '🐶', '🐱', '🐭', '🐹', '🐰', '🦊', '🐻', '🐼', '🐨', '🐯', '🦁', '🐮', '🐷', '🐽', '🐸', '🐵',
        '🙈', '🙉', '🙊', '🐒', '🐔', '🐧', '🐦', '🐤', '🐣', '🐥', '🦆', '🦅', '🦉', '🦇', '🐺', '🐗',
        '🐴', '🦄', '🐝', '🐛', '🦋', '🐌', '🐞', '🐜', '🦟', '🦗', '🕷️', '🕸️', '🦂', '🐢', '🐍', '🦎',
        '🦖', '🦕', '🐙', '🦑', '🦐', '🦞', '🦀', '🐡', '🐠', '🐟', '🐬', '🐳', '🐋', '🦈', '🐊', '🐅',
        '🌸', '🌺', '🌻', '🌹', '🥀', '🌷', '🌼', '🌾', '🍀', '☘️', '🍃', '🍂', '🍁', '🌿', '🌱', '🌲',
        '🌳', '🌴', '🌵', '🌾', '🌿', '🍀', '☘️', '🍃', '🍂', '🍁', '🪴', '🌾', '💐', '🏵️', '🌹', '🥀'
    ];
}

function getFoodEmojis() {
    return [
        '🍎', '🍐', '🍊', '🍋', '🍌', '🍉', '🍇', '🍓', '🫐', '🍈', '🍒', '🍑', '🥭', '🍍', '🥥', '🥝',
        '🍅', '🍆', '🥑', '🥦', '🥬', '🥒', '🌶️', '🫑', '🌽', '🥕', '🧄', '🧅', '🥔', '🍠', '🥐', '🥖',
        '🍞', '🥨', '🥯', '🧀', '🥚', '🍳', '🧈', '🥞', '🧇', '🥓', '🥩', '🍗', '🍖', '🦴', '🌭', '🍔',
        '🍟', '🍕', '🫓', '🥪', '🥙', '🧆', '🌮', '🌯', '🫔', '🥗', '🥘', '🫕', '🥫', '🍝', '🍜', '🍲',
        '🍛', '🍣', '🍱', '🥟', '🦪', '🍤', '🍙', '🍚', '🍘', '🍥', '🥠', '🥮', '🍢', '🍡', '🍧', '🍨'
    ];
}

function getSymbolsEmojis() {
    return [
        '❤️', '🧡', '💛', '💚', '💙', '💜', '🖤', '🤍', '🤎', '💔', '❣️', '💕', '💞', '💓', '💗', '💖',
        '💘', '💝', '💟', '☮️', '✝️', '☪️', '🕉️', '☸️', '✡️', '🔯', '🕎', '☯️', '☦️', '🛐', '⛎', '♈',
        '♉', '♊', '♋', '♌', '♍', '♎', '♏', '♐', '♑', '♒', '♓', '🆔', '⚛️', '🉑', '☢️', '☣️', '📴',
        '📳', '🈶', '🈚', '🈸', '🈺', '🈷️', '✴️', '🆚', '💮', '🉐', '㊙️', '㊗️', '🈴', '🈵', '🈹', '🈲',
        '🅰️', '🅱️', '🆎', '🆑', '🅾️', '🆘', '❌', '⭕', '🛑', '⛔', '📛', '🚫', '💯', '💢', '♨️', '🚷'
    ];
}

function setupEmojiPickerEvents() {
    const container = document.getElementById('emojiPickerContainer');
    if (!container) return;

    container.addEventListener('click', function (e) {
        const emojiItem = e.target.closest('.mx_EmojiPicker_item');
        if (emojiItem) {
            insertEmoji(emojiItem.dataset.emoji);
            e.stopPropagation();
            return;
        }

        const categoryBtn = e.target.closest('.mx_EmojiPicker_anchor');
        if (categoryBtn) {
            document.querySelectorAll('.mx_EmojiPicker_anchor').forEach(btn => btn.classList.remove('active'));
            categoryBtn.classList.add('active');

            document.querySelectorAll('.mx_EmojiPicker_category').forEach(cat => cat.classList.remove('active'));
            const targetCategory = container.querySelector(`.mx_EmojiPicker_category[data-category="${categoryBtn.dataset.category}"]`);
            if (targetCategory) targetCategory.classList.add('active');

            setTimeout(() => {
                adjustEmojiPickerPosition();
            }, 50);

            e.stopPropagation();
        }
    });
}

function insertEmoji(emoji) {
    const input = document.getElementById('messageInput');
    if (!input) return;

    const start = input.selectionStart;
    const end = input.selectionEnd;
    input.value = input.value.substring(0, start) + emoji + input.value.substring(end);
    input.focus();
    const newPos = start + emoji.length;
    input.setSelectionRange(newPos, newPos);

    setEmojiPickerVisible(false);
    const container = document.getElementById('emojiPickerContainer');
    if (container) container.style.display = 'none';
}