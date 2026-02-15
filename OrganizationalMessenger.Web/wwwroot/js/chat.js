// ============================================
// Chat Application - Master File
// همه ماژول‌ها از اینجا import می‌شوند
// ============================================

import './chat/variables.js';
import { initChat } from './chat/init.js';

// ✅ DOM آماده شد
document.addEventListener('DOMContentLoaded', function () {
    console.log('📄 DOM Loaded');
    initChat();
});