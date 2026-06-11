import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import "./index.css";
import Layout from "./components/Layout";
import Upload from "./pages/Upload";
import ChatList from "./pages/ChatList";
import ChatDetail from "./pages/ChatDetail";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<ChatList />} />
          <Route path="/upload" element={<Upload />} />
          <Route path="/chat/:id" element={<ChatDetail />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </React.StrictMode>
);
