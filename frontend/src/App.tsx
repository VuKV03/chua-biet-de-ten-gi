import { Toaster, toast } from "sonner";
import { BrowserRouter, Routes, Route } from "react-router";
import HomePage from "./pages/HomePage";
import NotFound from "./pages/NotFound";

function App() {

  return <>


    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="*" element={<NotFound />} />
      </Routes>
    </BrowserRouter>
  </>
}

export default App
