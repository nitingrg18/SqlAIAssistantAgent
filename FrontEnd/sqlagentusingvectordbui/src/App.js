import React, { useState, useEffect, useRef } from "react";
import "./App.css";

// --- Helper Components ---

// HistoryItem now includes a delete button
const HistoryItem = ({ conversation, onClick, onDelete, isActive }) => (
  <div className={`history-item ${isActive ? "active" : ""}`} onClick={onClick}>
    <div className="history-title">{conversation.title}</div>
    <button
      onClick={(e) => {
        e.stopPropagation();
        onDelete();
      }}
      className="delete-button"
    >
      ×
    </button>
  </div>
);

// MessageBubble component is unchanged
const MessageBubble = ({ msg }) => (
  <div className={`message-bubble ${msg.sender}`}>
    <div className="sender">{msg.sender === "user" ? "You" : "SQL Agent"}</div>
    <div className="content">
      {msg.isSql ? (
        <pre>
          <code>{msg.content}</code>
        </pre>
      ) : (
        <p>{msg.content}</p>
      )}
    </div>
  </div>
);

// --- Main App Component ---

function App() {
  const [conversations, setConversations] = useState(() => {
    try {
      const saved = localStorage.getItem("sqlAgentConversations");
      return saved ? JSON.parse(saved) : {};
    } catch (error) {
      return {};
    }
  });

  const [activeThreadId, setActiveThreadId] = useState(
    () => localStorage.getItem("sqlAgentActiveThreadId") || null
  );
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const chatHistoryRef = useRef(null);
  const API_URL = "https://localhost:7217/api/sqlagent/assistant";

  // --- useEffect Hooks for saving state and scrolling ---

  useEffect(() => {
    localStorage.setItem(
      "sqlAgentConversations",
      JSON.stringify(conversations)
    );
  }, [conversations]);

  useEffect(() => {
    if (activeThreadId) {
      localStorage.setItem("sqlAgentActiveThreadId", activeThreadId);
    } else {
      localStorage.removeItem("sqlAgentActiveThreadId");
    }
  }, [activeThreadId]);

  useEffect(() => {
    if (chatHistoryRef.current) {
      chatHistoryRef.current.scrollTop = chatHistoryRef.current.scrollHeight;
    }
  }, [conversations, activeThreadId, isLoading]);

  // --- Core Logic Functions ---

  const handleSendMessage = async () => {
    if (!input.trim() || isLoading) return;
    const question = input.trim();
    const threadIdForApiCall = activeThreadId;
    let tempIdForUi = activeThreadId;

    setIsLoading(true);
    setInput("");

    if (!threadIdForApiCall) {
      tempIdForUi = `temp_${Date.now()}`;
      setConversations((prev) => ({
        ...prev,
        [tempIdForUi]: {
          title: question.substring(0, 40) + "...",
          messages: [],
        },
      }));
      setActiveThreadId(tempIdForUi);
    }

    setConversations((prev) => {
      const conv = prev[tempIdForUi];
      const updatedMessages = [
        ...conv.messages,
        { sender: "user", content: question, isSql: false },
      ];
      return { ...prev, [tempIdForUi]: { ...conv, messages: updatedMessages } };
    });

    try {
      const requestBody = { question, threadId: threadIdForApiCall };
      const response = await fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(requestBody),
      });
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(
          `HTTP error! status: ${response.status} - ${errorText}`
        );
      }

      const data = await response.json();
      const realThreadId = data.threadId;
      const agentMessage = { sender: "agent", content: data.sql, isSql: true };

      setConversations((prev) => {
        const newConvs = { ...prev };
        const conv = newConvs[tempIdForUi];
        delete newConvs[tempIdForUi];
        newConvs[realThreadId] = {
          ...conv,
          messages: [...conv.messages, agentMessage],
        };
        return newConvs;
      });

      setActiveThreadId(realThreadId);
    } catch (error) {
      console.error("API call failed:", error);
      setConversations((prev) => {
        const conv = prev[tempIdForUi];
        const updatedMessages = [
          ...conv.messages,
          { sender: "agent", content: `Error: ${error.message}`, isSql: false },
        ];
        return {
          ...prev,
          [tempIdForUi]: { ...conv, messages: updatedMessages },
        };
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleNewChat = () => {
    setActiveThreadId(null);
    setInput("");
  };

  const switchConversation = (threadId) => {
    setActiveThreadId(threadId);
  };

  // --- NEW: Delete Functions ---

  const handleDeleteConversation = (threadIdToDelete) => {
    // A confirmation dialog is good practice
    if (window.confirm("Are you sure you want to delete this conversation?")) {
      setConversations((prev) => {
        const newConversations = { ...prev };
        delete newConversations[threadIdToDelete];
        return newConversations;
      });
      // If the deleted chat was the active one, go to the welcome screen
      if (activeThreadId === threadIdToDelete) {
        setActiveThreadId(null);
      }
    }
  };

  const handleDeleteAllHistory = () => {
    if (
      window.confirm(
        "Are you sure you want to delete ALL conversation history? This cannot be undone."
      )
    ) {
      setConversations({});
      setActiveThreadId(null);
    }
  };

  const handleKeyPress = (e) => e.key === "Enter" && handleSendMessage();
  const activeMessages =
    (activeThreadId && conversations[activeThreadId]?.messages) || [];

  return (
    <div className="app-layout">
      <div className="sidebar">
        <div className="sidebar-header">
          <h3>History</h3>
          <button
            onClick={handleNewChat}
            className="new-chat-button"
            title="New Chat"
          >
            +
          </button>
        </div>
        <div className="history-list">
          {Object.keys(conversations).map((threadId) => (
            <HistoryItem
              key={threadId}
              conversation={conversations[threadId]}
              onClick={() => switchConversation(threadId)}
              onDelete={() => handleDeleteConversation(threadId)} // Pass delete handler
              isActive={threadId === activeThreadId}
            />
          ))}
        </div>
        <div className="sidebar-footer">
          <button
            onClick={handleDeleteAllHistory}
            className="delete-all-button"
          >
            Clear All History
          </button>
        </div>
      </div>

      <div className="main-content">
        <header className="main-header">
          <h1>AI SQL Agent Studio</h1>
        </header>

        <div className="chat-area" ref={chatHistoryRef}>
          {activeThreadId ? (
            activeMessages.map((msg, index) => (
              <MessageBubble key={index} msg={msg} />
            ))
          ) : (
            <div className="welcome-screen">
              <h2>Start a new conversation</h2>
              <p>
                Ask a question to generate a SQL query, or select a conversation
                from the history panel.
              </p>
            </div>
          )}
          {isLoading && (
            <MessageBubble
              msg={{ sender: "agent", content: "...", isSql: false }}
            />
          )}
        </div>

        <div className="input-area">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyPress={handleKeyPress}
            className="chat-input"
            placeholder="Ask a question about the database..."
            disabled={isLoading}
          />
          <button
            onClick={handleSendMessage}
            className="send-button"
            disabled={isLoading}
            title="Send Message"
          >
            ➤
          </button>
        </div>
      </div>
    </div>
  );
}

export default App;
