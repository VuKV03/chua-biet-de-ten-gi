import React from 'react'

const NotFound = () => {
    return (
        <div className="flex flex-col items-center justify-center min-h-screen text-center bg-slate-50">
            <img src="404_NotFound.png" alt="404 Not Found" className="max-w-full mb-6 w-96" />

            <p className="text-2xl font-semibold text-slate-900">Bạn đã đi vào vùng cấm địa!!</p>

            <a href="/" className="inline-block px-6 py-3 mt-6 font-media text-white transition shadow-md 
            bg-primary rounded-2xl hover:bg-primary-dark">
                Quay về trang chủ
            </a>
        </div>
    )
}

export default NotFound