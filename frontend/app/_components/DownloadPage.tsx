'use client';

import React, { useState } from 'react';

export default function DownloadPage() {
  const [url, setUrl] = useState('');
  const [startMin, setStartMin] = useState('00');
  const [startSec, setStartSec] = useState('15');
  const [endMin, setEndMin] = useState('00');
  const [endSec, setEndSec] = useState('30');
  const [loading, setLoading] = useState(false);
  const [status, setStatus] = useState('');

  const handleDownload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!url) return alert('Por favor, insira uma URL válida do YouTube.');

    setLoading(true);
    setStatus('Processando e cortando o vídeo...');

    // Formata o tempo no padrão HH:MM:SS para a API em Python
    const inicio = `${startMin.padStart(2, '0')}:${startSec.padStart(2, '0')}`;
    const fim = `${endMin.padStart(2, '0')}:${endSec.padStart(2, '0')}`;

    try {
      // Substitua pela URL real da sua API em Python quando subir o backend
      const response = await fetch("http://localhost:8000/api/video/cortar", {
        method: 'POST',
        body: JSON.stringify({ url, inicio, fim }),
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) throw new Error('Erro ao processar o vídeo no servidor.');

      setStatus('Baixando arquivo...');

      // Recebe o arquivo blob (MP4) e força o download no navegador
      const blob = await response.blob();
      const downloadUrl = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = downloadUrl;
      a.download = `trecho_${inicio}_a_${fim}.mp4`;
      document.body.appendChild(a);
      a.click();
      a.remove();

      setStatus('');
    } catch (error) {
      console.error(error);
      setStatus('Ocorreu um erro ao tentar baixar o trecho.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col items-center justify-center p-4 selection:bg-indigo-500 selection:text-white">
      {/* Background Glow sutil */}
      <div className="absolute top-1/4 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[500px] h-[500px] bg-indigo-600/10 blur-[120px] rounded-full pointer-events-none" />

      <main className="w-full max-w-2xl bg-slate-900/60 border border-slate-800 backdrop-blur-md rounded-2xl p-6 md:p-8 shadow-2xl relative z-10">

        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-3xl md:text-4xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent">
            YUDecote
          </h1>
          <p className="text-slate-400 mt-2 text-sm md:text-base">
            Corte e Baixe Trechos de Vídeos do YouTube
            Cole o link, escolha o intervalo de tempo exato e faça o download do trecho em segundos.
          </p>
        </div>

        <form onSubmit={handleDownload} className="space-y-6">

          {/* Input da URL */}
          <div className="space-y-2">
            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider block">
              URL do Vídeo
            </label>
            <input
              type="url"
              placeholder="https://www.youtube.com/watch?v=..."
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-3 text-slate-200 placeholder-slate-600 focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 transition-all"
              required
            />
          </div>

          {/* Seletores de Tempo (Grid) */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">

            {/* Tempo de Início */}
            <div className="bg-slate-950/50 border border-slate-800/80 p-4 rounded-xl flex flex-col justify-between">
              <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider block mb-3">
                Tempo Inicial (MM:SS)
              </span>
              <div className="flex items-center gap-2 justify-center">
                <input
                  type="number"
                  min="0"
                  max="59"
                  value={startMin}
                  onChange={(e) => setStartMin(e.target.value)}
                  className="w-16 bg-slate-900 border border-slate-800 text-center py-2 text-xl font-bold rounded-lg focus:outline-none focus:border-indigo-500 text-indigo-400"
                />
                <span className="text-xl font-bold text-slate-600">:</span>
                <input
                  type="number"
                  min="0"
                  max="59"
                  value={startSec}
                  onChange={(e) => setStartSec(e.target.value)}
                  className="w-16 bg-slate-900 border border-slate-800 text-center py-2 text-xl font-bold rounded-lg focus:outline-none focus:border-indigo-500 text-indigo-400"
                />
              </div>
            </div>

            {/* Tempo de Fim */}
            <div className="bg-slate-950/50 border border-slate-800/80 p-4 rounded-xl flex flex-col justify-between">
              <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider block mb-3">
                Tempo Final (MM:SS)
              </span>
              <div className="flex items-center gap-2 justify-center">
                <input
                  type="number"
                  min="0"
                  max="59"
                  value={endMin}
                  onChange={(e) => setEndMin(e.target.value)}
                  className="w-16 bg-slate-900 border border-slate-800 text-center py-2 text-xl font-bold rounded-lg focus:outline-none focus:border-purple-500 text-purple-400"
                />
                <span className="text-xl font-bold text-slate-600">:</span>
                <input
                  type="number"
                  min="0"
                  max="59"
                  value={endSec}
                  onChange={(e) => setEndSec(e.target.value)}
                  className="w-16 bg-slate-900 border border-slate-800 text-center py-2 text-xl font-bold rounded-lg focus:outline-none focus:border-purple-500 text-purple-400"
                />
              </div>
            </div>

          </div>

          {/* Status Message */}
          {status && (
            <div className="text-center text-sm text-indigo-400 font-medium animate-pulse">
              {status}
            </div>
          )}

          {/* Botão de Submit */}
          <button
            type="submit"
            disabled={loading}
            className={`w-full py-4 rounded-xl font-bold text-white tracking-wide shadow-lg shadow-indigo-500/20 transition-all duration-200 
              ${loading
                ? 'bg-slate-800 text-slate-500 cursor-not-allowed'
                : 'bg-gradient-to-r from-indigo-500 to-purple-600 hover:from-indigo-600 hover:to-purple-700 active:scale-[0.99]'
              }`}
          >
            {loading ? (
              <div className="flex items-center justify-center gap-2">
                <svg className="animate-spin h-5 w-5 text-slate-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Processando...
              </div>
            ) : (
              'Cortar e Baixar Vídeo'
            )}
          </button>
        </form>
      </main>

      {/* Footer minimalista */}
      <footer className="mt-8 text-xs text-slate-600 tracking-wider">
        Desenvolvido com Next.js & Python FastAPI
      </footer>
    </div>
  );
}