// ============================================
// Global Variables
// ============================================

// SignalR Connection
export let connection = null;
export function setConnection(conn) {
    connection = conn;
}

// Current Chat
export let currentChat = null;
export function setCurrentChat(chat) {
    currentChat = chat;
}

// Typing
export let typingTimer = null;
export function setTypingTimer(timer) {
    typingTimer = timer;
}

// Emoji Picker
export let emojiPickerVisible = false;
export function setEmojiPickerVisible(visible) {
    emojiPickerVisible = visible;
}

// Message Loading
export let isLoadingMessages = false;
export function setIsLoadingMessages(loading) {
    isLoadingMessages = loading;
}

export let hasMoreMessages = true;
export function setHasMoreMessages(has) {
    hasMoreMessages = has;
}

export let lastSenderId = null;
export function setLastSenderId(id) {
    lastSenderId = id;
}

export let messageGroupCount = 0;
export function setMessageGroupCount(count) {
    messageGroupCount = count;
}

// Multi-Select
export let multiSelectMode = false;
export function setMultiSelectMode(mode) {
    multiSelectMode = mode;
}

export let selectedMessages = new Set();

// Reply
export let replyingToMessage = null;
export function setReplyingToMessage(msg) {
    replyingToMessage = msg;
}

// Page Focus
export let isPageFocused = true;
export function setIsPageFocused(focused) {
    isPageFocused = focused;
}

// Voice Recording
export let mediaRecorder = null;
export function setMediaRecorder(recorder) {
    mediaRecorder = recorder;
}

export let audioChunks = [];
export function setAudioChunks(chunks) {
    audioChunks = chunks;
}

export let isRecording = false;
export function setIsRecording(recording) {
    isRecording = recording;
}

export let recordingStartTime = null;
export function setRecordingStartTime(time) {
    recordingStartTime = time;
}

export let recordingTimer = null;
export function setRecordingTimer(timer) {
    recordingTimer = timer;
}

export let currentPlayingAudio = null;
export function setCurrentPlayingAudio(audio) {
    currentPlayingAudio = audio;
}

// Image Preview
export let isZoomed = false;
export function setIsZoomed(zoomed) {
    isZoomed = zoomed;
}

export let currentPreviewImage = null;
export function setCurrentPreviewImage(img) {
    currentPreviewImage = img;
}

// Message Settings
export let messageSettings = {
    allowEdit: true,
    allowDelete: true,
    editTimeLimit: 3600,
    deleteTimeLimit: 7200
};

export function setMessageSettings(settings) {
    messageSettings = settings;
}

// Message Status Enum
export const MessageStatus = { Sent: 1, Delivered: 2, Read: 3 };